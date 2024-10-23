using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using static ExtraProjectH.ExtraProjectH;

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
                CircleJig circleJig = new CircleJig(circle, centerPoint); // Đổi tên thành CircleJig

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

        [CommandMethod("ChiaDeuDuongThang")]
        public void ChiaDeuDuongThang()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

          //VẪN ĐANG LÀM
            
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