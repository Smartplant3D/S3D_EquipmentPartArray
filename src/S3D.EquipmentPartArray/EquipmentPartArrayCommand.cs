using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Ingr.SP3D.Common.Client;
using Ingr.SP3D.Common.Client.Services;
using Ingr.SP3D.Common.Middle;
using Ingr.SP3D.Common.Middle.Services;
using Ingr.SP3D.Equipment.Middle;
using Ingr.SP3D.ReferenceData.Middle;
using S3D.EquipmentPartArray.Core;

namespace S3D.EquipmentPartArray
{
    [Guid("6D2C57A8-B6B9-4B47-966D-027AF25E786B")]
    [ProgId("S3D.EquipmentPartArray.EquipmentPartArrayCommand")]
    [ComVisible(true)]
    public sealed class EquipmentPartArrayCommand : BaseStepCommand
    {
        private readonly List<BusinessObject> sources = new List<BusinessObject>();
        private Equipment parent;
        private ArrayPanel panel;
        private GraphicViewHiliter preview;
        private ClientTransactionManager transaction;
        private PickKind pick;
        private V3 axisStart;
        private bool closing;
        private bool previewActive;

        public override int EnableUIFlags => (int)EnableUIFlagSettings.ActiveView;

        public override void OnStart(int commandID, object argument)
        {
            base.OnStart(commandID, argument);
#if !DISABLE_LICENSE_CHECK
            var result = Services.LicenseGuard.Validate();
            if (!result.IsValid) { MessageBox.Show(result.Message, "Equipment Part Array"); StopCommand(); return; }
#endif
            try
            {
                transaction = ClientServiceProvider.TransactionMgr;
                ConfigureSteps();
                preview = new GraphicViewHiliter { Color = (int)ColorConstants.RGBYellow, Weight = 3, LinePattern = Hiliter.HiliterLinePattern.Dotted };
                panel = new ArrayPanel(
                    text => MiddleServiceProvider.UOMMgr.ParseUnit(UnitType.Distance, text),
                    value => MiddleServiceProvider.UOMMgr.FormatUnit(UnitType.Distance, value));
                panel.Changed += UpdatePreview;
                panel.Confirmed += Create;
                panel.CaptureRequested += CaptureSelection;
                panel.PickRequested += StartPick;
                panel.FormClosed += (_, __) => { if (!closing) StopCommand(); };
                panel.Show();
                if (CurrentSelection().Count > 0) CaptureSelection();
                else panel.SetStatus("请选择部件或设备直属形体，再点击使用所选对象。 / Select parts or equipment-level shapes, then use selection.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Equipment Part Array");
                StopCommand();
            }
        }

        public override void OnStop()
        {
            closing = true;
            if (panel != null) { panel.Changed -= UpdatePreview; panel.Confirmed -= Create; panel.CaptureRequested -= CaptureSelection; panel.PickRequested -= StartPick; panel.Close(); panel.Dispose(); panel = null; }
            DiscardPreview();
            WriteStatusBarMsg("");
            base.OnStop();
        }

        public override void OnSuspend() { if (panel != null) panel.Hide(); base.OnSuspend(); }
        public override void OnResume() { if (panel != null) panel.Show(); base.OnResume(); }

        protected override void OnMouseDown(GraphicView view, GraphicViewManager.GraphicViewEventArgs e, Position position)
        {
            if (e.Button == GraphicViewManager.GraphicViewEventArgs.MouseButtons.Right) { StopCommand(); return; }
            if (pick == PickKind.None)
            {
                base.OnMouseDown(view, e, position);
                return;
            }
            if (e.Button != GraphicViewManager.GraphicViewEventArgs.MouseButtons.Left) return;
            var point = From(position);
            switch (pick)
            {
                case PickKind.Base: panel.SetPoint("Base", point); pick = PickKind.None; break;
                case PickKind.Center: panel.SetPoint("Center", point); pick = PickKind.None; break;
                case PickKind.AxisStart: axisStart = point; pick = PickKind.AxisEnd; WriteStatusBarMsg("拾取旋转轴第二点 / Pick second axis point"); break;
                case PickKind.AxisEnd: panel.SetPoint("Axis", point - axisStart); pick = PickKind.None; break;
            }
            if (pick == PickKind.None) { CurrentStepIndex = 0; WriteStatusBarMsg("选择部件或点击创建 / Select parts or click Create."); }
        }

        protected override void OnKeyDown(BaseGraphicCommand.KeyEventArgs e)
        {
            if (e.KeyValue == (int)ConsoleKey.Escape) StopCommand();
        }

        private void StartPick(PickKind kind)
        {
            if (sources.Count == 0) { panel.SetStatus("请先选择源对象并点击使用所选对象。 / Select sources and use selection first."); return; }
            pick = kind;
            CurrentStepIndex = 1;
            WriteStatusBarMsg(kind == PickKind.AxisStart ? "拾取旋转轴第一点 / Pick first axis point" : "在模型视图拾取点 / Pick point in model view");
        }

        private void ConfigureSteps()
        {
            StepCount = 2;
            Steps[0].Prompt = "选择设备部件或设备直属形体，再点击使用所选对象 / Select parts or equipment-level shapes, then use selection.";
            Steps[0].LocateBehavior = StepDefinition.LocateBehaviors.Fence;
            Steps[0].StepFilter = new StepFilter();
            Steps[0].UseStepHiliterForSelection = true;
            Steps[0].HiliteSelected = true;
            Steps[0].HiliteCumulativeSelection = true;
            Steps[0].SelectHiliterColor = (int)ColorConstants.RGBWhite;
            Steps[0].SelectHiliterWeight = 2;
            Steps[1].Prompt = "在模型视图拾取所需点 / Pick the requested point in the model view.";
            Steps[1].LocateBehavior = StepDefinition.LocateBehaviors.SmartSketch3D;
            Steps[1].SmartSketchOptions.CenterPoint = true;
            Steps[1].SmartSketchOptions.ReferenceAxisAlignment = true;
            Steps[1].SmartSketchOptions.Intersection = true;
            Steps[1].SmartSketchOptions.MidPoint = true;
            CurrentStepIndex = 0;
            Enabled = true;
        }

        private ReadOnlyBOCollection CurrentSelection()
        {
            var stepSelection = Steps[0].SelectedBusinessObjects;
            return stepSelection != null && stepSelection.Count > 0
                ? stepSelection : ClientServiceProvider.SelectSet.SelectedObjects.Clone();
        }

        private void CaptureSelection()
        {
            try
            {
                CurrentStepIndex = 0;
                pick = PickKind.None;
                ReadSelection();
                panel.SetSourceCount(sources.Count);
                V3 anchor = From(((ILocalCoordinateSystem)sources[0]).Origin);
                panel.SetPoint("Base", anchor);
                panel.SetPoint("Center", anchor);
                UpdatePreview();
                WriteStatusBarMsg("已获取源对象；调整参数或拾取点 / Sources captured; adjust parameters or pick points.");
            }
            catch (Exception ex)
            {
                sources.Clear(); parent = null;
                panel.SetSourceCount(0);
                DiscardPreview();
                panel.SetStatus("选择错误 / Selection error: " + ex.Message);
            }
        }

        private void ReadSelection()
        {
            sources.Clear(); parent = null;
            var selected = CurrentSelection();
            if (selected.Count == 0) throw new InvalidOperationException("请选择至少一个设备部件或设备直属 GenericShape。 / Select equipment components or equipment-level GenericShape objects.");
            foreach (BusinessObject item in selected)
            {
                var source = ResolveSource(item);
                if (sources.Any(existing => SameObject(existing, source))) continue;
                var owner = ((ISystemChild)source).SystemParent as Equipment;
                if (owner == null) throw new InvalidOperationException("源对象必须直属设备。 / Each source must be directly under an Equipment.");
                if (parent != null && !SameObject(parent, owner)) throw new InvalidOperationException("全部源对象必须属于同一台设备。 / All sources must belong to the same Equipment.");
                if ((source.AccessControl & AccessControlIDs.Read) == 0 || (source.AccessControl & AccessControlIDs.Update) == 0)
                    throw new InvalidOperationException("源对象不可读或不可编辑。 / A selected source is not readable/editable.");
                ValidateSourcePart(source);
                parent = owner; sources.Add(source);
            }
            if ((parent.AccessControl & AccessControlIDs.Create) == 0) throw new InvalidOperationException("没有在源设备下创建对象的权限。 / No create permission on source Equipment.");
        }

        private static BusinessObject ResolveSource(BusinessObject selected)
        {
            var direct = selected as EquipmentComponent;
            if (direct != null) return direct;

            // A graphic pick can return an equipment-level shape or a component's child shape.
            // EquipmentMiddleExample/CopyConstructor.vb verifies the shape's SystemParent.
            if (selected is GenericShape)
            {
                ISystem owner;
                try { owner = ((ISystemChild)selected).SystemParent; }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("无法确定所选对象的父级 / Cannot resolve parent of " + selected.GetType().FullName + ": " + ex.Message, ex);
                }
                var component = owner as EquipmentComponent;
                if (component != null) return component;
                if (owner is Equipment && selected is GenericShape && !(selected is PrismaticShape)) return selected;
                throw new InvalidOperationException("所选对象 / Selected " + selected.GetType().FullName +
                    " 属于 / belongs to " + (owner == null ? "无父级 / no parent" : owner.GetType().FullName) +
                    "。仅支持设备部件或设备直属 GenericShape。 / Only EquipmentComponent or equipment-level GenericShape is supported.");
            }

            throw new InvalidOperationException("不支持所选对象 / Unsupported selected object: " + selected.GetType().FullName +
                "。请选择设备部件或设备直属 GenericShape。 / Select EquipmentComponent or equipment-level GenericShape.");
        }

        private static void ValidateSourcePart(BusinessObject source)
        {
            var component = source as EquipmentComponent;
            if (component != null && !(component.Part is EquipmentComponentPart))
                throw new InvalidOperationException("源部件缺少支持的目录 EquipmentComponentPart。 / Source component has no supported catalog part.");
        }

        private static bool SameObject(BusinessObject first, BusinessObject second)
        {
            if (ReferenceEquals(first, second)) return true;
            if (first == null || second == null) return false;
            if (!string.IsNullOrEmpty(first.ObjectID) && !string.IsNullOrEmpty(second.ObjectID))
                return first.ObjectID == second.ObjectID && first.DatabaseID == second.DatabaseID;
            return first.Equals(second);
        }

        private Placement[] Plan()
        {
            if (panel.CircularMode) return ArrayPlanner.Circular(panel.CircularCount, panel.Number("Fill angle"), panel.Point("Axis"));
            V3 x = V3.XAxis, y = V3.YAxis, z = V3.ZAxis;
            if (panel.LocalDirections)
            {
                var matrix = parent.Matrix;
                x = From(matrix.Transform(new Vector(1, 0, 0)));
                y = From(matrix.Transform(new Vector(0, 1, 0)));
                z = From(matrix.Transform(new Vector(0, 0, 1)));
            }
            return ArrayPlanner.Rectangle(panel.Rows, panel.Columns, panel.Layers,
                panel.Distance("Column pitch"), panel.Distance("Row pitch"), panel.Distance("Layer pitch"), x, y, z);
        }

        private V3 Target(V3 source, Placement placement)
        {
            if (!panel.CircularMode) return source + placement.Offset;
            V3 center = panel.Point("Center");
            return center + ArrayPlanner.Rotate(source - center, panel.Point("Axis"), placement.AngleDegrees);
        }

        private void UpdatePreview()
        {
            if (panel == null || transaction == null) return;
            try
            {
                DiscardPreview();
                if (sources.Count == 0) { panel.SetStatus("请选择源对象并点击使用所选对象。 / Select sources and use selection."); return; }
                Placement[] plan = Plan();
                long additional = (long)plan.Length * sources.Count;
                if (additional > 10000) throw new ArgumentException("一次最多新增 10,000 个对象。 / At most 10,000 items may be added.");
                foreach (var placement in plan)
                    foreach (var source in sources)
                    {
                        V3 target = Target(From(((ILocalCoordinateSystem)source).Origin), placement);
                        previewActive = true;
                        const double marker = 0.05;
                        var line = new Line3d(To(target - V3.XAxis * marker), To(target + V3.XAxis * marker));
                        preview.HilitedObjects.Add(line);
                    }
                if (previewActive) transaction.Compute();
                panel.SetStatus("预计新增 / Additional items: " + additional + "（原点标记预览 / origin markers）");
            }
            catch (Exception ex)
            {
                DiscardPreview(); panel.SetStatus("参数无效 / Invalid parameters: " + ex.Message);
            }
        }

        private void Create()
        {
            long additional = 0;
            try
            {
                DiscardPreview();
                ValidateCapturedSources();
                Placement[] plan = Plan();
                additional = (long)plan.Length * sources.Count;
                if (additional > 10000) throw new ArgumentException("一次最多新增 10,000 个对象。 / At most 10,000 items may be added.");
                if (additional == 0) { panel.SetStatus("没有需要创建的副本。 / No copies requested."); return; }
                int originalPortCount = parent.SystemChildren.OfType<GenericPort>().Count();
                var created = new List<BusinessObject>();
                foreach (var placement in plan)
                    foreach (var source in sources)
                    {
                        BusinessObject copy;
                        var sourceComponent = source as EquipmentComponent;
                        if (sourceComponent != null)
                        {
                            copy = new EquipmentComponent((EquipmentComponentPart)sourceComponent.Part, parent);
                        }
                        else
                        {
                            copy = CreateShapeCopy((GenericShape)source, parent);
                        }
                        var sourcePose = (ILocalCoordinateSystem)source;
                        var copyPose = (ILocalCoordinateSystem)copy;
                        copyPose.Matrix = new Matrix4X4(sourcePose.Matrix);
                        V3 target = Target(From(sourcePose.Origin), placement);
                        copyPose.Origin = To(target);
                        if (panel.CircularMode && panel.RotateCopies)
                        {
                            var p = To(target); var t1 = new Matrix4X4(); var r = new Matrix4X4(); var t2 = new Matrix4X4(); var transform = new Matrix4X4();
                            t1.Translate(new Vector(-p.X, -p.Y, -p.Z));
                            r.Rotate(placement.AngleDegrees * Math.PI / 180, ToVector(panel.Point("Axis").Normalized));
                            t2.Translate(new Vector(p.X, p.Y, p.Z));
                            transform.MultiplyMatrix(t2); transform.MultiplyMatrix(r); transform.MultiplyMatrix(t1);
                            ((ITransform)copy).Transform(transform);
                        }
                        created.Add(copy);
                    }
                transaction.Compute();
                if (parent.SystemChildren.OfType<GenericPort>().Count() != originalPortCount ||
                    created.OfType<EquipmentComponent>().Any(component => component.SystemChildren.OfType<GenericPort>().Any()))
                    throw new InvalidOperationException("副本生成了管口或端口，整批操作已中止。 / Copy generated a nozzle or port; the array was aborted.");
                transaction.Commit("Equipment Part Array");
            }
            catch (Exception ex)
            {
                try { transaction.Abort(); }
                catch (Exception rollbackError)
                {
                    MessageBox.Show("创建失败，且无法确认回滚。重试前请检查模型。 / Creation failed and rollback could not be confirmed. Check the model before retrying.\n" +
                        ex + "\n回滚错误 / Rollback error: " + rollbackError, "设备部件阵列 / Equipment Part Array");
                    return;
                }
                MessageBox.Show("阵列创建失败；本次更改已中止。 / Array creation failed; changes were aborted.\n" + ex, "设备部件阵列 / Equipment Part Array");
                UpdatePreview();
                return;
            }
            MessageBox.Show("已创建 " + additional + " 个独立阵列对象。 / Created " + additional + " independent array items.", "设备部件阵列 / Equipment Part Array");
            StopCommand();
        }

        private static GenericShape CreateShapeCopy(GenericShape source, Equipment owner)
        {
            // The GenericShape(source) overload fails inside Smart 3D's private-BO
            // initialization for equipment-level shapes. Place a fresh occurrence
            // from the same catalog part, then transfer only writable scalar IJUA
            // instance attributes. Relationships and ports are never traversed.
            Part part = source.Part;
            if (part == null)
                throw new InvalidOperationException("所选 GenericShape 缺少目录 Part，无法安全重建形体。 / The selected GenericShape has no catalog Part.");
            var copy = new GenericShape(part, owner);
            foreach (PropertyValue value in source.GetAllProperties())
            {
                var info = value.PropertyInfo;
                if (info == null || info.InterfaceInfo == null ||
                    !info.InterfaceInfo.Name.StartsWith("IJUA", StringComparison.Ordinal) ||
                    info.Complex || info.MetadataReadOnly || value.ReadOnly || !value.Valid)
                    continue;
                try { copy.SetPropertyValue(value); }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("无法复制形体实例属性 / Cannot copy shape instance property " +
                        info.InterfaceInfo.Name + "." + info.Name + "；整批操作将回滚 / the array will be rolled back.", ex);
                }
            }
            return copy;
        }

        private void ValidateCapturedSources()
        {
            if (sources.Count == 0 || parent == null) throw new InvalidOperationException("请先选择源对象并点击使用所选对象。 / Select sources and use selection first.");
            if ((parent.AccessControl & AccessControlIDs.Create) == 0) throw new InvalidOperationException("没有在源设备下创建对象的权限。 / No create permission on source Equipment.");
            foreach (var source in sources)
            {
                if (!SameObject(((ISystemChild)source).SystemParent as BusinessObject, parent))
                    throw new InvalidOperationException("已获取的源对象不再属于源设备。 / A captured source no longer belongs to the source Equipment.");
                if ((source.AccessControl & AccessControlIDs.Read) == 0 || (source.AccessControl & AccessControlIDs.Update) == 0)
                    throw new InvalidOperationException("已获取的源对象不再可读或可编辑。 / A captured source is no longer readable/editable.");
                ValidateSourcePart(source);
            }
        }

        private void DiscardPreview()
        {
            if (preview != null) preview.HilitedObjects.Clear();
            if (!previewActive) return;
            previewActive = false;
            transaction.Abort();
        }
        private static V3 From(Position point) => new V3(point.X, point.Y, point.Z);
        private static V3 From(Vector vector) => new V3(vector.X, vector.Y, vector.Z);
        private static Position To(V3 point) => new Position(point.X, point.Y, point.Z);
        private static Vector ToVector(V3 value) => new Vector(value.X, value.Y, value.Z);
    }
}
