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

            List<Wall> walls = CreateWalls(doc, 10000, 5000, level1, level2);
            AddDoor(doc, level1, walls[0]);
            AddWindow(doc, level1, walls[1], 1200);
            AddWindow(doc, level1, walls[2], 1200);
            AddWindow(doc, level1, walls[3], 1200);
            // AddFootPrintRoof(doc, level2, walls);
            AddExtrusionRoof(doc, level2, walls);
            return Result.Succeeded;
        }


        private ExtrusionRoof AddExtrusionRoof(Document doc, Level level, List<Wall> walls, string Name = "Типовой - 400мм", string FamilyName = "Базовая крыша")
        {
            RoofType roofType = new FilteredElementCollector(doc).OfClass(typeof(RoofType))
               .OfType<RoofType>()
                .Where(x => x.Name.Equals(Name))
                .Where(x => x.FamilyName.Equals(FamilyName))
                 .FirstOrDefault();
            double wallW = walls[0].Width;
            double dt = wallW / 2;

            double roofThickness = roofType.get_Parameter(BuiltInParameter.ROOF_ATTR_DEFAULT_THICKNESS_PARAM).AsDouble();

            double koef = 0.2;//без этой поправки чуток заходит на стены, не пойму почему...
            LocationCurve curve = walls[1].Location as LocationCurve;
            XYZ p1 = curve.Curve.GetEndPoint(0);
            XYZ p2 = curve.Curve.GetEndPoint(1);
            XYZ p = ((p1 + p2) / 2) + new XYZ(0, 0, level.Elevation * 1.5);
            p1 += new XYZ(0, 0, level.Elevation + roofThickness + koef);
            p2 += new XYZ(0, 0, level.Elevation + roofThickness + koef);
            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(p1 + new XYZ(0, -dt, 0), p));
            curveArray.Append(Line.CreateBound(p, p2 + new XYZ(0, dt, 0)));

             

            ExtrusionRoof roof;
            using (var ts = new Transaction(doc, "add roof"))
            {
                ts.Start();
                ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
                double wallLength = walls[0].get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                roof = doc.Create.NewExtrusionRoof(curveArray, plane, level, roofType, p1.X + dt, p1.X - wallLength - dt);
                ts.Commit();
            }


            return roof;
        }


        private FootPrintRoof AddFootPrintRoof(Document doc, Level level, List<Wall> walls, string Name = "Типовой - 400мм", string FamilyName = "Базовая крыша")
        {
            RoofType roofType = new FilteredElementCollector(doc).OfClass(typeof(RoofType))
               .OfType<RoofType>()
                .Where(x => x.Name.Equals(Name))
                .Where(x => x.FamilyName.Equals(FamilyName))
                 .FirstOrDefault();

            double wallW = walls[0].Width;
            double dt = wallW / 2;
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dt, -dt, 0));
            points.Add(new XYZ(dt, -dt, 0));
            points.Add(new XYZ(dt, dt, 0));
            points.Add(new XYZ(-dt, dt, 0));
            points.Add(new XYZ(-dt, -dt, 0));

            ModelCurveArray modelCurveArray = new ModelCurveArray();
            var app = doc.Application;
            CurveArray footPrint = app.Create.NewCurveArray();
            for (int i = 0; i < 4; i++)
            {
                LocationCurve curve = walls[i].Location as LocationCurve;
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);
                Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
                footPrint.Append(line);
            }
            FootPrintRoof roof;
            using (var ts = new Transaction(doc, "add roof"))
            {
                ts.Start();
                roof = doc.Create.NewFootPrintRoof(footPrint, level, roofType, out modelCurveArray);
                ModelCurveArrayIterator iterator = modelCurveArray.ForwardIterator();
                iterator.Reset();
                while (iterator.MoveNext())
                {
                    ModelCurve modelCurve = iterator.Current as ModelCurve;
                    roof.set_DefinesSlope(modelCurve, true);
                    roof.set_SlopeAngle(modelCurve, 0.5);
                }
                ts.Commit();
            }


            return roof;
        }

        public FamilyInstance AddWindow(Document doc, Level level, Wall host, double _height, string Name = "0406 x 0610 мм", string FamilyName = "Фиксированные")
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

        public FamilyInstance AddDoor(Document doc, Level level, Wall host, string Name = "0915 x 2134 мм", string FamilyName = "Одиночные-Щитовые")
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
