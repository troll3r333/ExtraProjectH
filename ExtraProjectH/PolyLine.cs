using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace ExtraProjectH
{
    public class PolylineCommand : ExtraProjectH
    {
    
        // Phương thức vẽ polyline sử dụng EntityJig
        public class PolylineJig : EntityJig
        {
            private Polyline _polyline;
            private Point3d _currentPoint;
            private int _vertexIndex;

            public PolylineJig(Polyline polyline, Point3d startPoint) : base(polyline)
            {
                _polyline = polyline;
                _currentPoint = startPoint;
                _vertexIndex = 0; // Bắt đầu từ đỉnh đầu tiên
                _polyline.AddVertexAt(_vertexIndex, new Point2d(startPoint.X, startPoint.Y), 0, 0, 0); // Thêm điểm đầu tiên vào polyline
            }

            // Cập nhật tọa độ điểm hiện tại
            protected override bool Update()
            {
                if (_vertexIndex > 0) // Chỉ cập nhật từ đỉnh thứ 2 trở đi
                {
                    _polyline.SetPointAt(_vertexIndex, new Point2d(_currentPoint.X, _currentPoint.Y));
                }
                return true;
            }

            // Lấy điểm tiếp theo từ người dùng
            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                JigPromptPointOptions ppo = new JigPromptPointOptions("\nChọn điểm tiếp theo:");
                ppo.BasePoint = _polyline.GetPoint3dAt(_vertexIndex); // Sử dụng điểm hiện tại
                ppo.UseBasePoint = true; // Sử dụng base point để giúp người dùng xác định điểm tiếp theo

                // Thu thập tọa độ điểm từ người dùng
                PromptPointResult ppr = prompts.AcquirePoint(ppo);

                if (ppr.Status == PromptStatus.OK)
                {
                    if (_currentPoint != ppr.Value)
                    {
                        _currentPoint = ppr.Value; // Lưu điểm hiện tại
                        return SamplerStatus.OK;
                    }
                    else
                    {
                        return SamplerStatus.NoChange;
                    }
                }

                return SamplerStatus.Cancel;
            }

            // Thêm đỉnh tiếp theo vào Polyline
            public void AddNextVertex()
            {
                _vertexIndex++; // Tăng chỉ số đỉnh trước khi thêm
                _polyline.AddVertexAt(_vertexIndex, new Point2d(_currentPoint.X, _currentPoint.Y), 0, 0, 0);
            }
        }

        [CommandMethod("VePolylineTuNhien")]
        public void VePolylineTuNhien()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Khởi tạo một đối tượng Polyline mới
                Autodesk.AutoCAD.DatabaseServices.Polyline polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline();

                // Yêu cầu người dùng chọn điểm đầu tiên cho polyline
                PromptPointOptions ppo = new PromptPointOptions("\nChọn điểm đầu tiên cho Polyline:");
                PromptPointResult ppr = ed.GetPoint(ppo);

                if (ppr.Status == PromptStatus.OK)
                {
                    // Khởi tạo PolylineJig với điểm đầu tiên do người dùng chọn
                    PolylineJig jig = new PolylineJig(polyline, ppr.Value);

                    // Trong jig, điểm đầu tiên đã được thêm vào polyline

                    while (true)
                    {
                        PromptResult res = ed.Drag(jig);

                        if (res.Status == PromptStatus.OK)
                        {
                            // Thêm điểm tiếp theo vào Polyline khi người dùng xác nhận
                            jig.AddNextVertex();
                        }
                        else
                        {
                            // Kết thúc khi người dùng nhấn Enter
                            break;
                        }
                    }

                    // Thêm polyline vào bản vẽ nếu có ít nhất 2 đỉnh
                    if (polyline.NumberOfVertices > 1)
                    {
                        btr.AppendEntity(polyline);
                        tr.AddNewlyCreatedDBObject(polyline, true);
                    }
                    else
                    {
                        ed.WriteMessage("\nCần ít nhất 2 điểm để tạo Polyline.");
                    }
                }

                tr.Commit();
            }
        }
    }
}
