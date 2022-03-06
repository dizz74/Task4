using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task4
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDocument = uiApp.ActiveUIDocument;
            Document doc = uiDocument.Document;

            CreateWalls(doc, 10000, 5000);

            return Result.Succeeded;
        }


        
 
        public List<Wall> CreateWalls(Document doc,double _width,double _depth,string baseLevelName = "Уровень 1",string heightLevelName = "Уровень 2") {
            var levels = GetLevels(doc);
            Level baseLevel = GetLevel(levels, baseLevelName);
            Level heightLevel = GetLevel(levels, heightLevelName);

            double width = UnitUtils.ConvertToInternalUnits(_width, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(_depth, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));
            List<Wall> walls;
            using (var ts = new Transaction(doc, "new walls"))
            {
                ts.Start();
                walls = new List<Wall>();
                for (int i = 0; i < 4; i++)
                {
                    Line geomLine = Line.CreateBound(points[i], points[i + 1]);
                    Wall wall = Wall.Create(doc, geomLine, baseLevel.Id, false);
                    walls.Add(wall);
                    wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.Set(heightLevel.Id);
                }
                ts.Commit();
            }
            return walls;
        }


        public Level GetLevel(List<Level> levels,string levelName) {
          return  levels.Where(x => x.Name.Equals(levelName)).OfType<Level>().FirstOrDefault();
        }
        public List<Level> GetLevels(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

        }

    }

}
