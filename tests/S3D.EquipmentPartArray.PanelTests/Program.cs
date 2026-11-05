using System;
using System.Linq;
using System.Windows.Forms;
using S3D.EquipmentPartArray;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            using (var panel = new ArrayPanel(s => double.Parse(s), d => d.ToString()))
            {
                panel.Show(); Application.DoEvents();
                if (!panel.TopMost) throw new Exception("panel must stay on top");
                if (!panel.CircularMode) throw new Exception("circular mode must be the default");
                var capture = FindButton(panel, "使用所选部件/形体 / Use selection");
                var create = FindButton(panel, "创建 / Create");
                var scrolling = panel.Controls.OfType<Panel>().First(p => p.AutoScroll);
                scrolling.AutoScrollPosition = new System.Drawing.Point(0, scrolling.VerticalScroll.Maximum);
                Application.DoEvents();
                AssertOnScreen(panel, capture, "capture");
                AssertOnScreen(panel, create, "create");
                var mode = FindCombo(panel);
                AssertOnScreen(panel, mode, "mode");
                if (FindLabel(panel, "行数 / Rows").Visible || !FindLabel(panel, "中心 X / Center X").Visible)
                    throw new Exception("default circular mode shows rectangular fields");
                mode.SelectedIndex = 0; Application.DoEvents();
                if (!FindLabel(panel, "行数 / Rows").Visible || FindLabel(panel, "中心 X / Center X").Visible)
                    throw new Exception("rectangular mode shows circular fields");
                if (create.Enabled) throw new Exception("Create enabled without captured sources");
                panel.SetSourceCount(1);
                if (!create.Enabled) throw new Exception("Create disabled after capturing a source");
                if (args.Length == 1)
                {
                    mode.SelectedIndex = 1; Application.DoEvents();
                    using (var image = new System.Drawing.Bitmap(panel.Width, panel.Height))
                    {
                        panel.DrawToBitmap(image, new System.Drawing.Rectangle(0, 0, panel.Width, panel.Height));
                        image.Save(args[0]);
                    }
                }
                panel.Close();
            }
            using (var units = new ArrayPanel(s => double.Parse(s) * 0.001, d => (d * 1000).ToString()))
            {
                int changes = 0;
                units.Changed += () => changes++;
                units.SetPoint("Center", new S3D.EquipmentPartArray.Core.V3(1, 2, 3));
                if (changes != 1) throw new Exception("setting a point emitted more than one preview update");
                if (Math.Abs(units.Distance("Column pitch") - 0.001) > 1e-12)
                    throw new Exception("distance did not use the project unit parser");
                if (Math.Abs(units.Point("Axis").Z - 1) > 1e-12)
                    throw new Exception("axis must remain dimensionless");
            }
            Console.WriteLine("Panel action visibility passed"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static Button FindButton(Control parent, string text)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Button button && button.Text == text) return button;
            var nested = FindButton(child, text);
            if (nested != null) return nested;
        }
        return null;
    }

    private static ComboBox FindCombo(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is ComboBox combo && combo.Items.Count > 0 && (string)combo.Items[0] == "矩形 / Rectangular") return combo;
            var nested = FindCombo(child);
            if (nested != null) return nested;
        }
        return null;
    }

    private static Label FindLabel(Control parent, string text)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Label label && label.Text == text) return label;
            var nested = FindLabel(child, text);
            if (nested != null) return nested;
        }
        return null;
    }

    private static void AssertOnScreen(Form form, Control button, string name)
    {
        if (button == null) throw new Exception(name + " button missing");
        var bounds = button.RectangleToScreen(button.ClientRectangle);
        var formBounds = form.RectangleToScreen(form.ClientRectangle);
        if (!formBounds.Contains(bounds)) throw new Exception(name + " button is outside visible panel");
    }
}
