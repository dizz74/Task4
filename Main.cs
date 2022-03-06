using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
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
            var levels = GetLevels(doc);
            Level level1 = GetLevelByName(levels, "Уровень 1");
            Level level2 = GetLevelByName(levels, "Уровень 2");

            List<Wall> walls = CreateWalls(doc, 10000, 5000,level1,level2);
            AddDoor(doc, level1, walls[0]);
            AddWindow(doc, level1, walls[1],1200);
            AddWindow(doc, level1, walls[2],1200);
            AddWindow(doc, level1, walls[3],1200);
            return Result.Succeeded;
        }


        public FamilyInstance AddWindow(Document doc, Level level, Wall host, double _height,string Name = "0406 x 0610 мм", string FamilyName = "Фиксированные")
        {
            double height = UnitUtils.ConvertToInternalUnits(_height, UnitTypeId.Millimeters);
            FamilyInstance door;
            FamilySymbol fSymbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals(Name))
                .Where(x => x.FamilyName.Equals(FamilyName))
                .FirstOrDefault();
            var curve = host.Location as LocationCurve;
            XYZ p1 = curve.Curve.GetEndPoint(0);
            XYZ p2 = curve.Curve.GetEndPoint(1);
            XYZ p = (p1 + p2) / 2;

            using (var ts = new Transaction(doc, "create window"))
            {
                ts.Start();
                if (!fSymbol.IsActive)
                {
                    fSymbol.Activate();
                    doc.Regenerate();
                }

                
                door = doc.Create.NewFamilyInstance(p, fSymbol, host, level, StructuralType.NonStructural);
                door.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM)?.Set(height);
                ts.Commit();
            }
            return door;
        }

        public FamilyInstance AddDoor(Document doc, Level level, Wall host,string Name = "0915 x 2134 мм",string FamilyName = "Одиночные-Щитовые")
        {
            FamilyInstance door;
            FamilySymbol fSymbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals(Name))
                .Where(x => x.FamilyName.Equals(FamilyName))
                .FirstOrDefault();
            var curve = host.Location as LocationCurve;
            XYZ p1 = curve.Curve.GetEndPoint(0);
            XYZ p2 = curve.Curve.GetEndPoint(1);
            XYZ p = (p1 + p2) / 2;

            using (var ts = new Transaction(doc, "create door"))
            {
                ts.Start();
                if (!fSymbol.IsActive)
                {
                    fSymbol.Activate();
                    doc.Regenerate();
                } 

                door = doc.Create.NewFamilyInstance(p, fSymbol, host, level, StructuralType.NonStructural);
                ts.Commit();
            }
            return door;
        }



        public List<Wall> CreateWalls(Document doc, double _width, double _depth, Level baseLevel, Level heightLevel)
        {
            var levels = GetLevels(doc);
          

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


        public Level GetLevelByName(List<Level> levels, string levelName)
        {
            return levels.Where(x => x.Name.Equals(levelName)).OfType<Level>().FirstOrDefault();
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
