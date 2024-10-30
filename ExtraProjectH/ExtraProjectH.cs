using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;

namespace ExtraProjectH
{
    public class ExtraProjectH
    {
        private Point3d GetPoint(Editor ed, string message)
        {
            // Nhập điểm từ người dùng
            PromptPointOptions ppo = new PromptPointOptions(message);
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                throw new System.Exception("Không nhận được điểm hợp lệ.");
            }
            return ppr.Value;
        }

        public class LineJig : EntityJig
        {
            private Point3d _startPoint;
            private Point3d _endPoint;

            public LineJig(Line line, Point3d startPoint) : base(line)
            {
                this._startPoint = startPoint;
                this._endPoint = startPoint; // Điểm cuối ban đầu giống điểm đầu
            }

            protected override bool Update()
            {
                Line line = Entity as Line;
                if (line != null)
                {
                    line.StartPoint = _startPoint;
                    line.EndPoint = _endPoint;
                    return true;
                }
                return false;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                JigPromptPointOptions pointOptions = new JigPromptPointOptions("\nChọn điểm cuối:");
                pointOptions.BasePoint = _startPoint;
                pointOptions.UseBasePoint = true;

                PromptPointResult pointResult = prompts.AcquirePoint(pointOptions);

                if (pointResult.Status == PromptStatus.OK)
                {
                    if (pointResult.Value != _endPoint)
                    {
                        _endPoint = pointResult.Value;
                        return SamplerStatus.OK;
                    }
                    else
                    {
                        return SamplerStatus.NoChange;
                    }
                }

                return SamplerStatus.Cancel;
            }
        }

        [CommandMethod("VeDuongThang")]
        public void VeDuongThang()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Lấy điểm bắt đầu của đường thẳng
                PromptPointResult ppr = ed.GetPoint("\nNhập điểm bắt đầu của đường thẳng:");
                if (ppr.Status != PromptStatus.OK) return;

                // Tạo đối tượng Line với điểm đầu và điểm cuối tạm thời giống nhau
                Line line = new Line(ppr.Value, ppr.Value);

                // Tạo đối tượng LineJig và cho phép người dùng chọn điểm cuối
                LineJig lineJig = new LineJig(line, ppr.Value);
                PromptResult res = ed.Drag(lineJig);

                if (res.Status == PromptStatus.OK)
                {
                    // Thêm đường thẳng vào bản vẽ nếu người dùng hoàn tất thành công
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                    tr.Commit();
                }
            }
        }

        public class CircleJig : EntityJig
        {
            private Point3d _centerPoint;
            private double _radius;

            public CircleJig(Circle circle, Point3d centerPoint) : base(circle) // truyền đối tượng Circle vào EntityJig

            {
                _radius = 0;
                _centerPoint = centerPoint;
            }

            // Cập nhật bán kính cho hình tròn
            protected override bool Update()
            {
                Circle circle = Entity as Circle;
                if (circle != null && _radius > 0)
                {
                    circle.Radius = _radius;
                    return true;
                }
                return false;
            }

            // Thu thập thông tin từ người dùng
            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                JigPromptDistanceOptions distOptions = new JigPromptDistanceOptions("\nChọn bán kính:");
                distOptions.BasePoint = _centerPoint;
                distOptions.UseBasePoint = true;

                PromptDoubleResult result = prompts.AcquireDistance(distOptions);

                if (result.Status == PromptStatus.OK)
                {
                    if (result.Value != _radius && result.Value > 0)
                    {
                        _radius = result.Value;
                        return SamplerStatus.OK;
                    }
                    else
                    {
                        return SamplerStatus.NoChange;
                    }
                }

                return SamplerStatus.Cancel;
            }
        }

        [CommandMethod("Vehinhcau")]
        public void Vehinhcau()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Point3d centerPoint = GetPoint(ed, "Nhập tâm hình tròn");
                // Khởi tạo đối tượng Circle với bán kính tạm thời là 0
                Circle circle = new Circle(centerPoint, Vector3d.ZAxis, 0);
                // Khởi tạo CircleJig để cho phép người dùng chọn bán kính
                CircleJig circleJig = new CircleJig(circle, centerPoint);
                // Thực hiện quá trình jig để người dùng nhập bán kính
                PromptResult result = ed.Drag(circleJig);
                // Nếu người dùng xác nhận bán kính, thêm hình tròn vào bản vẽ
                if (result.Status == PromptStatus.OK)
                {
                    btr.AppendEntity(circle);
                    tr.AddNewlyCreatedDBObject(circle, true);
                    tr.Commit();
                }
            }
        }


        [CommandMethod("ChiaDeuDoanDuocChon")]
        public void ChiaDeuDoanDuocChon()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Yêu cầu người dùng chọn một đối tượng (đường thẳng, đường tròn hoặc polyline)
                PromptEntityOptions peo = new PromptEntityOptions("\nChọn đường thẳng, đường tròn hoặc polyline để chia:");
                peo.SetRejectMessage("\nĐối tượng phải là đường thẳng, đường tròn hoặc polyline.");
                peo.AddAllowedClass(typeof(Line), true);
                peo.AddAllowedClass(typeof(Circle), true);
                peo.AddAllowedClass(typeof(Polyline), true); // Cho phép chọn polyline
                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK) return;

                // Yêu cầu người dùng nhập số phần cần chia
                PromptIntegerOptions pio = new PromptIntegerOptions("\nNhập số phần cần chia:");
                pio.AllowZero = false;
                pio.AllowNegative = false;
                PromptIntegerResult pir = ed.GetInteger(pio);

                if (pir.Status != PromptStatus.OK || pir.Value <= 1) return;

                int soPhan = pir.Value;

                // Xử lý nếu đối tượng được chọn là đường thẳng
                if (per.ObjectId.ObjectClass == RXObject.GetClass(typeof(Line)))
                {
                    Line line = (Line)tr.GetObject(per.ObjectId, OpenMode.ForWrite);
                    Vector3d vector = (line.EndPoint - line.StartPoint) / soPhan;

                    for (int i = 0; i < soPhan; i++)
                    {
                        Point3d startPoint = line.StartPoint + (vector * i);
                        Point3d endPoint = line.StartPoint + (vector * (i + 1));
                        Line newLine = new Line(startPoint, endPoint);
                        btr.AppendEntity(newLine);
                        tr.AddNewlyCreatedDBObject(newLine, true);
                    }
                    line.Erase();
                }
                // Xử lý nếu đối tượng được chọn là đường tròn
                else if (per.ObjectId.ObjectClass == RXObject.GetClass(typeof(Circle)))
                {
                    Circle circle = (Circle)tr.GetObject(per.ObjectId, OpenMode.ForWrite);
                    double angleStep = 2 * Math.PI / soPhan;
                    Point3d center = circle.Center;
                    double radius = circle.Radius;

                    for (int i = 1; i <= soPhan; i++)
                    {
                        double angle = i * angleStep;
                        Arc arc = new Arc(
                            center,
                            radius,
                            (i - 1) * angleStep,
                            i * angleStep);
                        btr.AppendEntity(arc);
                        tr.AddNewlyCreatedDBObject(arc, true);
                    }
                    circle.Erase();
                }
                // Xử lý nếu đối tượng được chọn là polyline
                else if (per.ObjectId.ObjectClass == RXObject.GetClass(typeof(Polyline)))
                {
                    Polyline polyline = (Polyline)tr.GetObject(per.ObjectId, OpenMode.ForWrite);
                    int vertexCount = polyline.NumberOfVertices;

                    // Nếu polyline có ít hơn 2 đỉnh, không thể chia
                    if (vertexCount < 2)
                    {
                        ed.WriteMessage("\nPolyline cần ít nhất 2 đỉnh để chia.");
                        return;
                    }

                    // Tính chiều dài tổng của polyline
                    double totalLength = 0.0;
                    for (int i = 0; i < vertexCount - 1; i++)
                    {
                        totalLength += polyline.GetPoint3dAt(i).DistanceTo(polyline.GetPoint3dAt(i + 1));
                    }

                    // Tính độ dài mỗi đoạn
                    double segmentLength = totalLength / soPhan;
                    double accumulatedLength = 0.0;

                    // Tạo các đoạn đường thẳng từ các điểm chia
                    Point3d previousPoint = polyline.GetPoint3dAt(0); // Điểm đầu
                    double currentLength = 0.0;

                    for (int i = 1; i <= soPhan; i++)
                    {
                        accumulatedLength = i * segmentLength;

                        // Tìm điểm tương ứng với độ dài đã tích lũy
                        for (int j = 0; j < vertexCount - 1; j++)
                        {
                            double segmentDistance = polyline.GetPoint3dAt(j).DistanceTo(polyline.GetPoint3dAt(j + 1));
                            currentLength += segmentDistance;

                            if (currentLength >= accumulatedLength)
                            {
                                // Tính toán điểm trên đoạn thẳng
                                double fraction = (accumulatedLength - (currentLength - segmentDistance)) / segmentDistance;
                                Point3d currentPoint = new Point3d(
                                    polyline.GetPoint3dAt(j).X * (1 - fraction) + polyline.GetPoint3dAt(j + 1).X * fraction,
                                    polyline.GetPoint3dAt(j).Y * (1 - fraction) + polyline.GetPoint3dAt(j + 1).Y * fraction,
                                    polyline.GetPoint3dAt(j).Z * (1 - fraction) + polyline.GetPoint3dAt(j + 1).Z * fraction
                                );

                                Line newLine = new Line(previousPoint, currentPoint);
                                btr.AppendEntity(newLine);
                                tr.AddNewlyCreatedDBObject(newLine, true);

                                previousPoint = currentPoint;
                                break;
                            }
                        }
                        currentLength = 0.0; // Đặt lại chiều dài cho đoạn tiếp theo
                    }

                    polyline.Erase(); // Xóa polyline ban đầu
                }

                tr.Commit();
            }
        }

        // Hàm tính toán tọa độ của điểm trên đường tròn tại góc angle
        private Point3d PointOnCircle(Point3d center, double radius, double angle)
        {
            return new Point3d(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle),
                center.Z);
        }

        [CommandMethod("Xoamoithu")]
        public void XoaBalloon()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (ObjectId objId in btr)
                {
                    Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                    ent.Erase();
                }

                tr.Commit();
            }
        }
    }
}