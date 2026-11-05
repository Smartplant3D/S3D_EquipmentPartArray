using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using S3D.EquipmentPartArray.Core;

namespace S3D.EquipmentPartArray
{
    internal enum PickKind { None, Base, Center, AxisStart, AxisEnd }

    internal sealed class ArrayPanel : Form
    {
        private static readonly Dictionary<string, string> captions = new Dictionary<string, string>
        {
            ["Mode"] = "模式 / Mode", ["Directions"] = "方向 / Directions",
            ["Rows"] = "行数 / Rows", ["Columns"] = "列数 / Columns", ["Layers"] = "层数 / Layers",
            ["Column pitch"] = "列间距 / Column pitch", ["Row pitch"] = "行间距 / Row pitch",
            ["Layer pitch"] = "层间距 / Layer pitch", ["Base X"] = "基点 X / Base X",
            ["Base Y"] = "基点 Y / Base Y", ["Base Z"] = "基点 Z / Base Z",
            ["Center X"] = "中心 X / Center X", ["Center Y"] = "中心 Y / Center Y",
            ["Center Z"] = "中心 Z / Center Z", ["Axis X"] = "轴向 X / Axis X",
            ["Axis Y"] = "轴向 Y / Axis Y", ["Axis Z"] = "轴向 Z / Axis Z",
            ["Circular count"] = "环形数量 / Count", ["Fill angle"] = "填充角 / Fill angle",
            ["Orientation"] = "朝向 / Orientation", ["Pick base"] = "拾取基点 / Pick base",
            ["Pick center"] = "拾取中心 / Pick center", ["Pick axis (2 pts)"] = "拾取旋转轴 / Pick axis (2 pts)"
        };

        public event Action Changed;
        public event Action Confirmed;
        public event Action CaptureRequested;
        public event Action<PickKind> PickRequested;
        private readonly Dictionary<string, TextBox> fields = new Dictionary<string, TextBox>();
        private readonly NumericUpDown rows, columns, layers, circularCount;
        private readonly ComboBox mode, coordinate;
        private readonly CheckBox rotate;
        private readonly TextBox status;
        private readonly Label sourceCount;
        private readonly Button confirm;
        private readonly Func<string, double> parseDistance;
        private readonly Func<double, string> formatDistance;
        private bool suppressChanged;

        public ArrayPanel(Func<string, double> parseDistance, Func<double, string> formatDistance)
        {
            this.parseDistance = parseDistance ?? throw new ArgumentNullException(nameof(parseDistance));
            this.formatDistance = formatDistance ?? throw new ArgumentNullException(nameof(formatDistance));
            Text = "S3D 设备部件阵列 / Equipment Part Array"; Width = 500; Height = 690; MinimumSize = new Size(460, 520);
            TopMost = true;
            FormBorderStyle = FormBorderStyle.SizableToolWindow; StartPosition = FormStartPosition.CenterScreen;
            var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 110, ColumnCount = 2, RowCount = 3, Padding = new Padding(10, 5, 10, 5) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            sourceCount = new Label { Text = "源对象 / Sources: 0", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            header.Controls.Add(sourceCount, 0, 0); header.SetColumnSpan(sourceCount, 2);
            var capture = new Button { Text = "使用所选部件/形体 / Use selection", Dock = DockStyle.Fill };
            header.Controls.Add(capture, 0, 1); header.SetColumnSpan(capture, 2);
            capture.Click += (_, __) => CaptureRequested?.Invoke();
            header.Controls.Add(new Label { Text = Caption("Mode"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
            mode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            mode.Items.AddRange(new object[] { "矩形 / Rectangular", "环形 / Circular" }); mode.SelectedIndex = 1;
            header.Controls.Add(mode, 1, 2);
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 124, Padding = new Padding(10, 5, 10, 6) };
            status = new TextBox { Text = "请选择设备部件或设备直属形体。 / Select components or equipment-level shapes.", Dock = DockStyle.Fill,
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle };
            confirm = new Button { Text = "创建 / Create", Dock = DockStyle.Bottom, Height = 32, Enabled = false };
            footer.Controls.Add(status); footer.Controls.Add(confirm);
            confirm.Click += (_, __) => Confirmed?.Invoke();
            var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var rectangular = NewModeLayout();
            var circular = NewModeLayout();
            body.Controls.Add(rectangular); body.Controls.Add(circular);
            Controls.Add(body); Controls.Add(footer); Controls.Add(header);
            coordinate = AddCombo(rectangular, "Directions", "全局 / Global", "设备局部 / Equipment local");
            rows = AddCount(rectangular, "Rows", 2); columns = AddCount(rectangular, "Columns", 2); layers = AddCount(rectangular, "Layers", 1);
            AddField(rectangular, "Column pitch", "1"); AddField(rectangular, "Row pitch", "1"); AddField(rectangular, "Layer pitch", "1");
            AddField(rectangular, "Base X", "0"); AddField(rectangular, "Base Y", "0"); AddField(rectangular, "Base Z", "0");
            AddPick(rectangular, "Pick base", PickKind.Base);
            AddField(circular, "Center X", "0"); AddField(circular, "Center Y", "0"); AddField(circular, "Center Z", "0");
            AddPick(circular, "Pick center", PickKind.Center);
            AddField(circular, "Axis X", "0"); AddField(circular, "Axis Y", "0"); AddField(circular, "Axis Z", "1");
            AddPick(circular, "Pick axis (2 pts)", PickKind.AxisStart);
            circularCount = AddCount(circular, "Circular count", 4);
            AddField(circular, "Fill angle", "360");
            rotate = new CheckBox { Text = "副本随阵列旋转 / Rotate copies", Checked = true, AutoSize = true };
            AddControl(circular, "Orientation", rotate); rotate.CheckedChanged += (_, __) => RaiseChanged();
            rectangular.Visible = false;
            mode.SelectedIndexChanged += (_, __) =>
            {
                rectangular.Visible = !CircularMode;
                circular.Visible = CircularMode;
                body.AutoScrollPosition = new Point(0, 0);
                RaiseChanged();
            };
            Shown += (_, __) => RaiseChanged();
        }

        public bool CircularMode => mode.SelectedIndex == 1;
        public bool LocalDirections => coordinate.SelectedIndex == 1;
        public bool RotateCopies => rotate.Checked;
        public int Rows => (int)rows.Value; public int Columns => (int)columns.Value; public int Layers => (int)layers.Value;
        public int CircularCount => (int)circularCount.Value;
        public double Number(string label) => double.Parse(fields[label].Text, NumberStyles.Float, CultureInfo.CurrentCulture);
        public double Distance(string label) => parseDistance(fields[label].Text);
        public V3 Point(string name)
        {
            bool axis = name == "Axis";
            return new V3(axis ? Number(name + " X") : Distance(name + " X"),
                axis ? Number(name + " Y") : Distance(name + " Y"),
                axis ? Number(name + " Z") : Distance(name + " Z"));
        }
        public void SetPoint(string name, V3 point)
        {
            bool axis = name == "Axis";
            suppressChanged = true;
            try
            {
                fields[name + " X"].Text = axis ? point.X.ToString("G17", CultureInfo.CurrentCulture) : formatDistance(point.X);
                fields[name + " Y"].Text = axis ? point.Y.ToString("G17", CultureInfo.CurrentCulture) : formatDistance(point.Y);
                fields[name + " Z"].Text = axis ? point.Z.ToString("G17", CultureInfo.CurrentCulture) : formatDistance(point.Z);
            }
            finally { suppressChanged = false; }
            RaiseChanged();
        }
        public void SetStatus(string text) => status.Text = text;
        public void SetSourceCount(int count)
        {
            sourceCount.Text = "源对象 / Sources: " + count;
            confirm.Enabled = count > 0;
        }
        private void RaiseChanged() { if (!suppressChanged) Changed?.Invoke(); }
        private static TableLayoutPanel NewModeLayout()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            return layout;
        }
        private static void AddControl(TableLayoutPanel layout, string label, Control control)
        {
            int row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label { Text = Caption(label), AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            layout.Controls.Add(control, 1, row); layout.SetColumnSpan(control, 2);
        }
        private ComboBox AddCombo(TableLayoutPanel layout, string label, params string[] items)
        {
            var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            box.Items.AddRange(items); box.SelectedIndex = 0; AddControl(layout, label, box);
            box.SelectedIndexChanged += (_, __) => RaiseChanged(); return box;
        }
        private NumericUpDown AddCount(TableLayoutPanel layout, string label, int initial)
        {
            var box = new NumericUpDown { Minimum = 1, Maximum = 10000, Value = initial };
            AddControl(layout, label, box); box.ValueChanged += (_, __) => RaiseChanged(); return box;
        }
        private void AddField(TableLayoutPanel layout, string label, string initial)
        {
            var box = new TextBox { Text = initial };
            fields.Add(label, box); AddControl(layout, label, box); box.TextChanged += (_, __) => RaiseChanged();
        }
        private void AddPick(TableLayoutPanel layout, string label, PickKind kind)
        {
            var button = new Button { Text = Caption(label), AutoSize = true };
            AddControl(layout, "", button); button.Click += (_, __) => PickRequested?.Invoke(kind);
        }
        private static string Caption(string key) => captions.TryGetValue(key, out var value) ? value : key;
    }
}
