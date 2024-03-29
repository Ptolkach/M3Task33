﻿using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M3Task33
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));

            using (Transaction ts = new Transaction(doc, "Add parameter"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Длина с запасом", categorySet, BuiltInParameterGroup.PG_DATA, true);
                ts.Commit();
            }


            IList<Reference> selectedElementRefList = uidoc.Selection.PickObjects(ObjectType.Element, new PipeFilter(), "Выберите трубы");

            using (Transaction ts = new Transaction(doc, "Set parameters"))
            {
                ts.Start();
                foreach (var selectedElement in selectedElementRefList)
                {
                    var element = doc.GetElement(selectedElement);
                    Parameter lengthParameter = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                    Parameter stLengthParameter = element.LookupParameter("Длина с запасом");
                    stLengthParameter.Set(lengthParameter.AsDouble() * 1.1);
                }
                ts.Commit();
            }

            return Result.Succeeded;
        }

        private void CreateSharedParameter(Application application,
            Document doc, string parameterName, 
            CategorySet categorySet, BuiltInParameterGroup builtInParameterGroup, bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
            }
            Definition definition = definitionFile.Groups.
                SelectMany(group => group.Definitions).
                FirstOrDefault(def => def.Name.Equals(parameterName));
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;
            }

            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
            {
                binding = application.Create.NewInstanceBinding(categorySet);
            }

            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builtInParameterGroup);
        }

    }
}
