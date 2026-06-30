using SunshineSteamAppManager.Models;

namespace SunshineSteamAppManager.Forms;

public sealed class PreviewDialog : Form
{
    private PreviewDialog(PreviewPlan plan, bool confirm, bool restartSunshineAfterApply)
    {
        Text = confirm ? "Review changes" : "Preview changes";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(640, 520);
        Size = new Size(760, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = confirm ? "Ready to update Sunshine" : "Here is what would change",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };

        var details = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Font = new Font("Segoe UI", 10F),
            Text = BuildPreviewText(plan, restartSunshineAfterApply)
        };

        var note = new Label
        {
            Text = "A backup will be made before the app list is saved.",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0, 10, 0, 10)
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var primaryButton = new Button
        {
            Text = confirm ? "Apply changes" : "Close",
            AutoSize = true,
            DialogResult = DialogResult.OK,
            Padding = new Padding(12, 4, 12, 4)
        };
        buttons.Controls.Add(primaryButton);

        if (confirm)
        {
            var cancelButton = new Button
            {
                Text = "Not yet",
                AutoSize = true,
                DialogResult = DialogResult.Cancel,
                Padding = new Padding(12, 4, 12, 4),
                Margin = new Padding(0, 0, 8, 0)
            };
            buttons.Controls.Add(cancelButton);
            CancelButton = cancelButton;
        }

        AcceptButton = primaryButton;
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(details, 0, 1);
        layout.Controls.Add(note, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);
    }

    public static bool ShowPreview(IWin32Window owner, PreviewPlan plan, bool confirm, bool restartSunshineAfterApply)
    {
        using var dialog = new PreviewDialog(plan, confirm, restartSunshineAfterApply);
        var result = dialog.ShowDialog(owner);
        return !confirm || result == DialogResult.OK;
    }

    private static string BuildPreviewText(PreviewPlan plan, bool restartSunshineAfterApply)
    {
        var lines = new List<string>();
        AddGroup(lines, "Games to add", plan.AppsToAdd);
        AddGroup(lines, "Games already added but selected", plan.AppsToUpdate);
        AddGroup(lines, "Games to fix", plan.AppsToRepair);
        AddGroup(lines, "Covers to download", plan.CoversToDownload);
        AddGroup(lines, "Old managed games found", plan.StaleManagedApps);

        lines.Add("Backup");
        lines.Add(string.IsNullOrWhiteSpace(plan.ProposedBackupPath)
            ? "  A backup path will be chosen when changes are applied."
            : $"  {plan.ProposedBackupPath}");
        lines.Add("");
        lines.Add("Sunshine restart");
        lines.Add(restartSunshineAfterApply
            ? "  The app will ask before restarting Sunshine."
            : "  Sunshine will not be restarted automatically.");

        return string.Join(Environment.NewLine, lines);
    }

    private static void AddGroup(List<string> lines, string title, IReadOnlyList<string> items)
    {
        lines.Add(title);
        if (items.Count == 0)
        {
            lines.Add("  None");
        }
        else
        {
            foreach (var item in items.Take(25))
            {
                lines.Add("  " + item);
            }

            if (items.Count > 25)
            {
                lines.Add($"  ...and {items.Count - 25} more");
            }
        }

        lines.Add("");
    }
}
