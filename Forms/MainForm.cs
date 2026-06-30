using System.ComponentModel;
using System.Diagnostics;
using SunshineSteamAppManager.Logging;
using SunshineSteamAppManager.Models;
using SunshineSteamAppManager.Services;
using SunshineSteamAppManager.Steam;
using SunshineSteamAppManager.SteamGridDb;
using SunshineSteamAppManager.Storage;
using SunshineSteamAppManager.Sunshine;

namespace SunshineSteamAppManager.Forms;

public sealed class MainForm : Form
{
    private readonly OperationLogger _logger = new();
    private readonly SettingsStorage _settingsStorage = new();
    private readonly ManagedStateStorage _managedStateStorage = new();
    private readonly SteamLocator _steamLocator = new();
    private readonly SunshineAppService _sunshineAppService = new();
    private readonly BackupService _backupService = new();
    private readonly BindingList<SteamGameRow> _steamRows = new();
    private readonly BindingList<SunshineAppRow> _sunshineRows = new();
    private readonly BindingList<BackupItem> _backupRows = new();

    private AppSettings _settings = AppSettings.CreateDefault();
    private CancellationTokenSource? _operationCancellation;

    private TextBox _steamInstallPathTextBox = null!;
    private TextBox _libraryFoldersPathTextBox = null!;
    private TextBox _appsJsonPathTextBox = null!;
    private TextBox _coverFolderTextBox = null!;
    private TextBox _apiKeyTextBox = null!;
    private TextBox _logTextBox = null!;
    private CheckBox _removeStaleCheckBox = null!;
    private CheckBox _refreshCoversCheckBox = null!;
    private CheckBox _preserveNamesCheckBox = null!;
    private CheckBox _restartSunshineCheckBox = null!;
    private TextBox _homeApiKeyTextBox = null!;
    private Label _homeSteamStatusLabel = null!;
    private Label _homeSunshineStatusLabel = null!;
    private Label _homeCoverStatusLabel = null!;
    private Label _homeScanStatusLabel = null!;
    private Label _homeApplyStatusLabel = null!;
    private DataGridView _homeSteamGrid = null!;
    private DataGridView _steamGrid = null!;
    private DataGridView _sunshineGrid = null!;
    private ListBox _backupListBox = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public MainForm()
    {
        Text = "Sunshine Steam App Manager";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1250, 1120);

        BuildUi();
        WireEvents();
    }

    private void BuildUi()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(10, 4)
        };

        tabs.TabPages.Add(BuildHomeTab());
        tabs.TabPages.Add(BuildAdvancedModeTab());
        tabs.TabPages.Add(BuildHelpTab());

        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready");
        statusStrip.Items.Add(_statusLabel);

        Controls.Add(tabs);
        Controls.Add(statusStrip);
    }

    private TabPage BuildHomeTab()
    {
        var page = new TabPage("Home")
        {
            BackColor = Color.White
        };

        var scroller = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.White
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Color.White
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 6; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        layout.Controls.Add(BuildHomeHeader(), 0, 0);
        layout.Controls.Add(BuildHomeSetupStep(), 0, 1);
        layout.Controls.Add(BuildHomeCoverStep(), 0, 2);
        layout.Controls.Add(BuildHomeScanStep(), 0, 3);
        layout.Controls.Add(BuildHomeGameGrid(), 0, 4);
        layout.Controls.Add(BuildHomeApplyStep(), 0, 5);

        scroller.Controls.Add(layout);
        page.Controls.Add(scroller);
        return page;
    }

    private TabPage BuildAdvancedModeTab()
    {
        var page = new TabPage("Advanced Mode");
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(10, 4)
        };

        tabs.TabPages.Add(BuildSetupTab());
        tabs.TabPages.Add(BuildSteamGamesTab());
        tabs.TabPages.Add(BuildSunshineAppsTab());
        tabs.TabPages.Add(BuildBackupsLogsTab());
        tabs.TabPages.Add(BuildAdvancedTab());

        page.Controls.Add(tabs);
        return page;
    }

    private static Control BuildHomeHeader()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 72,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 8)
        };
        var title = new Label
        {
            Text = "Add Steam games to Sunshine",
            AutoSize = true,
            Location = new Point(0, 0),
            Font = new Font("Segoe UI", 20F, FontStyle.Bold)
        };
        var subtitle = new Label
        {
            Text = "Follow the steps below. Advanced Mode keeps the full detailed tools.",
            AutoSize = true,
            Location = new Point(2, 43),
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = Color.FromArgb(80, 80, 80)
        };
        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        return panel;
    }

    private Control BuildHomeSetupStep()
    {
        var group = CreateHomeGroup("1. Set up Sunshine", "Find Steam and check where Sunshine keeps its app list.");
        var buttons = CreateButtonRow();
        AddButton(buttons, "Auto-detect Steam", async () => await AutoDetectSteamAsync());
        AddButton(buttons, "Check setup", async () => await ValidateSettingsAsync(showMessage: true));
        AddButton(buttons, "Save setup", async () => await SaveSettingsAsync());
        _homeSteamStatusLabel = CreateStatusLabel("Steam not checked yet");
        _homeSunshineStatusLabel = CreateStatusLabel("Sunshine app list not checked yet");
        var statusRow = CreateButtonRow();
        statusRow.Controls.Add(_homeSteamStatusLabel);
        statusRow.Controls.Add(_homeSunshineStatusLabel);
        group.Controls.Add(buttons, 0, 2);
        group.Controls.Add(statusRow, 0, 3);
        return group;
    }

    private Control BuildHomeCoverStep()
    {
        var group = CreateHomeGroup("2. Set up cover pictures", "Covers need a free SteamGridDB account. You can skip this and still add games.");
        var instructions = new Label
        {
            Text = "Create or sign in to SteamGridDB, open the API page, generate an API key, then paste it here.",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(70, 70, 70),
            Margin = new Padding(0, 0, 0, 6)
        };
        var inputRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4,
            Margin = new Padding(0)
        };
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _homeApiKeyTextBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Paste SteamGridDB API key here" };
        inputRow.Controls.Add(_homeApiKeyTextBox, 0, 0);
        AddTableButton(inputRow, "Open API page", 1, OpenSteamGridDbApiPage);
        AddTableButton(inputRow, "Save key", 2, async () => await SaveCoverKeyAsync());
        AddTableButton(inputRow, "Test key", 3, async () => await TestCoverKeyAsync());
        _homeCoverStatusLabel = CreateStatusLabel("Key needed");
        group.Controls.Add(instructions, 0, 2);
        group.Controls.Add(inputRow, 0, 3);
        group.Controls.Add(_homeCoverStatusLabel, 0, 4);
        return group;
    }

    private Control BuildHomeScanStep()
    {
        var group = CreateHomeGroup("3. Scan and choose games", "Scan Steam, then tick the games you want to add or fix.");
        var buttons = CreateButtonRow();
        AddButton(buttons, "Scan Steam games", async () => await ScanSteamGamesAsync());
        AddButton(buttons, "Select ready games", () => SelectRowsByStatus("New", "Needs Repair"));
        AddButton(buttons, "Download covers", async () => await DownloadCoversForCheckedRowsAsync());
        _homeScanStatusLabel = CreateStatusLabel("No scan yet");
        buttons.Controls.Add(_homeScanStatusLabel);
        group.Controls.Add(buttons, 0, 2);
        return group;
    }

    private Control BuildHomeGameGrid()
    {
        _homeSteamGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            Height = 260,
            MinimumSize = new Size(0, 220),
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            DataSource = _steamRows,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false
        };
        _homeSteamGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.Import),
            HeaderText = "Add",
            FillWeight = 36
        });
        _homeSteamGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.Name),
            HeaderText = "Game",
            ReadOnly = true,
            FillWeight = 180
        });
        _homeSteamGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.FriendlyCoverStatus),
            HeaderText = "Cover",
            ReadOnly = true,
            FillWeight = 70
        });
        _homeSteamGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.FriendlySunshineStatus),
            HeaderText = "Sunshine",
            ReadOnly = true,
            FillWeight = 90
        });
        ApplyGridStyle(_homeSteamGrid);
        return _homeSteamGrid;
    }

    private Control BuildHomeApplyStep()
    {
        var group = CreateHomeGroup("4. Preview and apply", "Check what will change, then update Sunshine.");
        var buttons = CreateButtonRow();
        AddButton(buttons, "Preview changes", async () => await PreviewCheckedRowsAsync());
        AddButton(buttons, "Apply selected", async () => await ApplyCheckedSteamRowsAsync(forceRepair: false));
        _homeApplyStatusLabel = CreateStatusLabel("A backup will be made before changes are saved.");
        buttons.Controls.Add(_homeApplyStatusLabel);
        group.Controls.Add(buttons, 0, 2);
        return group;
    }

    private static TableLayoutPanel CreateHomeGroup(string title, string subtitle)
    {
        var group = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 10),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        group.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 2)
        }, 0, 0);
        group.Controls.Add(new Label
        {
            Text = subtitle,
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(70, 70, 70),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 1);
        return group;
    }

    private static FlowLayoutPanel CreateButtonRow()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0)
        };
    }

    private static Label CreateStatusLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(70, 70, 70),
            Padding = new Padding(8, 6, 8, 4),
            Margin = new Padding(4, 0, 4, 0)
        };
    }

    private static void AddTableButton(TableLayoutPanel panel, string text, int column, Func<Task> action)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(8, 3, 8, 3) };
        button.Click += async (_, _) => await action();
        panel.Controls.Add(button, column, 0);
    }

    private static void AddTableButton(TableLayoutPanel panel, string text, int column, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(8, 3, 8, 3) };
        button.Click += (_, _) => action();
        panel.Controls.Add(button, column, 0);
    }

    private static void ApplyGridStyle(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 248);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 40, 40);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(218, 235, 255);
        grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
        grid.RowTemplate.Height = 28;
    }

    private TabPage BuildSetupTab()
    {
        var page = new TabPage("Setup");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 3,
            RowCount = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        _steamInstallPathTextBox = AddPathRow(layout, 0, "Steam install path", BrowseFolder);
        _libraryFoldersPathTextBox = AddPathRow(layout, 1, "libraryfolders.vdf path", BrowseFile);
        _appsJsonPathTextBox = AddPathRow(layout, 2, "Sunshine apps.json path", BrowseFileOrNewJson);
        _coverFolderTextBox = AddPathRow(layout, 3, "Cover output folder", BrowseFolder);

        layout.Controls.Add(new Label
        {
            Text = "SteamGridDB API key",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 4);
        _apiKeyTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = true
        };
        layout.Controls.Add(_apiKeyTextBox, 1, 4);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        var autoDetectButton = new Button { Text = "Auto-detect Steam", AutoSize = true };
        autoDetectButton.Click += async (_, _) => await AutoDetectSteamAsync();
        var validateButton = new Button { Text = "Validate settings", AutoSize = true };
        validateButton.Click += async (_, _) => await ValidateSettingsAsync(showMessage: true);
        var saveButton = new Button { Text = "Save settings", AutoSize = true };
        saveButton.Click += async (_, _) => await SaveSettingsAsync();
        buttons.Controls.Add(autoDetectButton);
        buttons.Controls.Add(validateButton);
        buttons.Controls.Add(saveButton);

        layout.Controls.Add(buttons, 1, 5);
        layout.SetColumnSpan(buttons, 2);

        var defaults = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text =
                "Default Steam paths checked:\r\n" +
                @"C:\Program Files (x86)\Steam" + "\r\n" +
                @"C:\Program Files\Steam" + "\r\n" +
                "Registry InstallPath values\r\n\r\n" +
                "Default Sunshine paths:\r\n" +
                @"C:\Program Files\Sunshine\config\apps.json" + "\r\n" +
                @"C:\Program Files\Sunshine\config\covers"
        };
        layout.Controls.Add(defaults, 1, 6);
        layout.SetColumnSpan(defaults, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildSteamGamesTab()
    {
        var page = new TabPage("Steam Games");
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 8)
        };

        AddButton(buttons, "Scan Steam games", async () => await ScanSteamGamesAsync());
        AddButton(buttons, "Download covers", async () => await DownloadCoversForCheckedRowsAsync());
        AddButton(buttons, "Preview changes", async () => await PreviewCheckedRowsAsync());
        AddButton(buttons, "Apply selected", async () => await ApplyCheckedSteamRowsAsync(forceRepair: false));
        AddButton(buttons, "Select all new", () => SelectRowsByStatus("New"));
        AddButton(buttons, "Deselect all", () => SelectNoSteamRows());
        AddButton(buttons, "Repair selected", async () => await ApplyCheckedSteamRowsAsync(forceRepair: true));

        _steamGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            DataSource = _steamRows,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _steamGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.Import),
            HeaderText = "Import",
            FillWeight = 45
        });
        _steamGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.AppId),
            HeaderText = "Steam AppID",
            ReadOnly = true,
            FillWeight = 75
        });
        _steamGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.Name),
            HeaderText = "Game name",
            ReadOnly = true,
            FillWeight = 145
        });
        _steamGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.InstallPath),
            HeaderText = "Install path",
            ReadOnly = true,
            FillWeight = 230
        });
        _steamGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.CoverStatus),
            HeaderText = "Cover status",
            ReadOnly = true,
            FillWeight = 80
        });
        _steamGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SteamGameRow.SunshineStatus),
            HeaderText = "Sunshine status",
            ReadOnly = true,
            FillWeight = 95
        });

        ApplyGridStyle(_steamGrid);
        panel.Controls.Add(_steamGrid);
        panel.Controls.Add(buttons);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildSunshineAppsTab()
    {
        var page = new TabPage("Sunshine Apps");
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 8)
        };

        AddButton(buttons, "Load current Sunshine apps", async () => await LoadSunshineAppsGridAsync());
        AddButton(buttons, "Repair detected Steam URI apps", async () => await RepairSelectedSunshineAppsAsync());
        AddButton(buttons, "Remove managed Steam apps", async () => await RemoveSelectedManagedSunshineAppsAsync());

        _sunshineGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            DataSource = _sunshineRows,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true
        };
        _sunshineGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SunshineAppRow.AppName),
            HeaderText = "App name",
            FillWeight = 140
        });
        _sunshineGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SunshineAppRow.Command),
            HeaderText = "Command",
            FillWeight = 180
        });
        _sunshineGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SunshineAppRow.ImagePath),
            HeaderText = "Image path",
            FillWeight = 180
        });
        _sunshineGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SunshineAppRow.IsSteamUriApp),
            HeaderText = "Steam URI",
            FillWeight = 55
        });
        _sunshineGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SunshineAppRow.IsManagedByThisTool),
            HeaderText = "Managed",
            FillWeight = 55
        });
        _sunshineGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SunshineAppRow.NeedsRepair),
            HeaderText = "Needs repair",
            FillWeight = 70
        });

        ApplyGridStyle(_sunshineGrid);
        panel.Controls.Add(_sunshineGrid);
        panel.Controls.Add(buttons);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildBackupsLogsTab()
    {
        var page = new TabPage("Backups / Logs");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 230
        };

        var backupPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 8)
        };

        AddButton(buttons, "Refresh backup list", RefreshBackupList);
        AddButton(buttons, "Create manual backup", async () => await CreateManualBackupAsync());
        AddButton(buttons, "Restore selected backup", async () => await RestoreSelectedBackupAsync());
        AddButton(buttons, "Open log folder", OpenLogFolder);

        _backupListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            DataSource = _backupRows,
            DisplayMember = nameof(BackupItem.FilePath)
        };

        backupPanel.Controls.Add(_backupListBox);
        backupPanel.Controls.Add(buttons);

        _logTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false
        };

        split.Panel1.Controls.Add(backupPanel);
        split.Panel2.Controls.Add(_logTextBox);
        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildAdvancedTab()
    {
        var page = new TabPage("Advanced");
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(18),
            AutoScroll = true,
            WrapContents = false
        };

        _removeStaleCheckBox = new CheckBox
        {
            Text = "Remove stale managed Steam apps after confirmation",
            AutoSize = true
        };
        _refreshCoversCheckBox = new CheckBox
        {
            Text = "Refresh existing cover art",
            AutoSize = true
        };
        _preserveNamesCheckBox = new CheckBox
        {
            Text = "Preserve manually edited Sunshine app names",
            AutoSize = true,
            Checked = true
        };
        _restartSunshineCheckBox = new CheckBox
        {
            Text = "Restart Sunshine after applying changes",
            AutoSize = true,
            Checked = true
        };

        layout.Controls.Add(_removeStaleCheckBox);
        layout.Controls.Add(_refreshCoversCheckBox);
        layout.Controls.Add(_preserveNamesCheckBox);
        layout.Controls.Add(_restartSunshineCheckBox);

        page.Controls.Add(layout);
        return page;
    }


    private static TabPage BuildHelpTab()
    {
        var page = new TabPage("Help");
        var helpText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Font = new Font("Segoe UI", 10F),
            Text =
                "Sunshine Steam App Manager helps you add your installed Steam games to Sunshine. Start on the Home tab and follow the steps from top to bottom.\r\n\r\n" +
                "Quick Start\r\n\r\n" +
                "1. On Home, click Auto-detect Steam.\r\n" +
                "2. Click Check setup. If something is missing, open Advanced Mode and choose the correct file or folder.\r\n" +
                "3. Set up cover pictures if you want game artwork. This is recommended, but you can skip it.\r\n" +
                "4. Click Scan Steam games.\r\n" +
                "5. Tick the games you want to add or fix.\r\n" +
                "6. Click Download covers if you set up a SteamGridDB key.\r\n" +
                "7. Click Preview changes to review the update.\r\n" +
                "8. Click Apply selected when you are ready.\r\n\r\n" +
                "How to Get Cover Pictures\r\n\r\n" +
                "1. Create or sign in to a free SteamGridDB account.\r\n" +
                "2. On Home, click Open API page.\r\n" +
                "3. On the SteamGridDB page, generate an API key.\r\n" +
                "4. Copy the API key and paste it into the box in the cover pictures step.\r\n" +
                "5. Click Save key, then Test key.\r\n" +
                "6. When the status says Covers ready, click Download covers after scanning games.\r\n\r\n" +
                "What the Statuses Mean\r\n\r\n" +
                "Ready to add means the game is not in Sunshine yet.\r\n" +
                "Already added means the game is already in Sunshine.\r\n" +
                "Can be fixed means the game exists, but this app can clean up its launch settings.\r\n" +
                "Cover missing means no picture has been downloaded yet.\r\n\r\n" +
                "Backups\r\n\r\n" +
                "A backup is made before changes are saved. You can restore backups in Advanced Mode under Backups / Logs.\r\n\r\n" +
                "Advanced Mode\r\n\r\n" +
                "Advanced Mode has the detailed setup fields, full game list, Sunshine app list, backups, logs, and extra options."
        };

        page.Controls.Add(helpText);
        return page;
    }
    private TextBox AddPathRow(TableLayoutPanel layout, int row, string label, Func<string?, string?> browse)
    {
        layout.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        var textBox = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(textBox, 1, row);

        var button = new Button { Text = "Browse...", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            var selected = browse(textBox.Text);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                textBox.Text = selected;
            }
        };
        layout.Controls.Add(button, 2, row);

        return textBox;
    }

    private static void AddButton(FlowLayoutPanel panel, string text, Func<Task> action)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(8, 3, 8, 3) };
        button.Click += async (_, _) => await action();
        panel.Controls.Add(button);
    }

    private static void AddButton(FlowLayoutPanel panel, string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(8, 3, 8, 3) };
        button.Click += (_, _) => action();
        panel.Controls.Add(button);
    }

    private void WireEvents()
    {
        Load += async (_, _) => await LoadInitialStateAsync();
        FormClosing += (_, _) => _operationCancellation?.Cancel();
        _logger.MessageLogged += (_, line) =>
        {
            if (IsDisposed)
            {
                return;
            }

            BeginInvoke(() =>
            {
                _logTextBox.AppendText(line + Environment.NewLine);
            });
        };

        _steamGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_steamGrid.IsCurrentCellDirty)
            {
                _steamGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                RefreshHomeStatus();
            }
        };

        _homeSteamGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_homeSteamGrid.IsCurrentCellDirty)
            {
                _homeSteamGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                RefreshHomeStatus();
            }
        };
    }

    private async Task LoadInitialStateAsync()
    {
        _settings = await _settingsStorage.LoadAsync();
        PushSettingsToUi();
        await LoadExistingLogAsync();
        RefreshBackupList();
        await _logger.LogAsync("Sunshine Steam App Manager opened.");
    }

    private async Task LoadExistingLogAsync()
    {
        if (!File.Exists(AppDataPaths.LogPath))
        {
            return;
        }

        try
        {
            var text = await File.ReadAllTextAsync(AppDataPaths.LogPath);
            _logTextBox.Text = text;
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.ScrollToCaret();
        }
        catch
        {
            // The live logger will still work if the previous log cannot be read.
        }
    }

    private void PushSettingsToUi()
    {
        _steamInstallPathTextBox.Text = _settings.SteamInstallPath;
        _libraryFoldersPathTextBox.Text = _settings.SteamLibraryFoldersPath;
        _appsJsonPathTextBox.Text = _settings.SunshineAppsJsonPath;
        _coverFolderTextBox.Text = _settings.CoverOutputFolder;
        _apiKeyTextBox.Text = _settings.SteamGridDbApiKey;
        _homeApiKeyTextBox.Text = _settings.SteamGridDbApiKey;
        _removeStaleCheckBox.Checked = _settings.AdvancedOptions.RemoveStaleManagedApps;
        _refreshCoversCheckBox.Checked = _settings.AdvancedOptions.RefreshExistingCoverArt;
        _preserveNamesCheckBox.Checked = _settings.AdvancedOptions.PreserveManualSunshineAppNames;
        _restartSunshineCheckBox.Checked = _settings.AdvancedOptions.RestartSunshineAfterApply;
        RefreshHomeStatus();
    }

    private void CaptureSettingsFromUi()
    {
        _settings.SteamInstallPath = _steamInstallPathTextBox.Text.Trim();
        _settings.SteamLibraryFoldersPath = _libraryFoldersPathTextBox.Text.Trim();
        _settings.SunshineAppsJsonPath = _appsJsonPathTextBox.Text.Trim();
        _settings.CoverOutputFolder = _coverFolderTextBox.Text.Trim();
        var homeApiKey = _homeApiKeyTextBox.Text.Trim();
        var advancedApiKey = _apiKeyTextBox.Text.Trim();
        _settings.SteamGridDbApiKey = !string.Equals(homeApiKey, _settings.SteamGridDbApiKey, StringComparison.Ordinal)
            ? homeApiKey
            : advancedApiKey;
        _homeApiKeyTextBox.Text = _settings.SteamGridDbApiKey;
        _apiKeyTextBox.Text = _settings.SteamGridDbApiKey;
        _settings.AdvancedOptions.RemoveStaleManagedApps = _removeStaleCheckBox.Checked;
        _settings.AdvancedOptions.RefreshExistingCoverArt = _refreshCoversCheckBox.Checked;
        _settings.AdvancedOptions.PreserveManualSunshineAppNames = _preserveNamesCheckBox.Checked;
        _settings.AdvancedOptions.RestartSunshineAfterApply = _restartSunshineCheckBox.Checked;
    }

    private void OpenSteamGridDbApiPage()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.steamgriddb.com/profile/preferences/api",
            UseShellExecute = true
        });
    }

    private async Task SaveCoverKeyAsync()
    {
        _apiKeyTextBox.Text = _homeApiKeyTextBox.Text.Trim();
        CaptureSettingsFromUi();
        await _settingsStorage.SaveAsync(_settings);
        await _logger.LogAsync(string.IsNullOrWhiteSpace(_settings.SteamGridDbApiKey) ? "SteamGridDB key cleared." : "SteamGridDB key saved.");
        RefreshHomeStatus();
        MessageBox.Show(this, string.IsNullOrWhiteSpace(_settings.SteamGridDbApiKey) ? "Cover key cleared." : "Cover key saved.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task TestCoverKeyAsync()
    {
        _apiKeyTextBox.Text = _homeApiKeyTextBox.Text.Trim();
        CaptureSettingsFromUi();
        if (string.IsNullOrWhiteSpace(_settings.SteamGridDbApiKey))
        {
            _homeCoverStatusLabel.Text = "Key needed";
            MessageBox.Show(this, "Paste your SteamGridDB API key first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var keyWorks = false;
        await RunOperationAsync("Testing SteamGridDB key...", async cancellationToken =>
        {
            var client = new SteamGridDbClient(_logger);
            keyWorks = await client.TestApiKeyAsync(_settings.SteamGridDbApiKey, cancellationToken);
            _homeCoverStatusLabel.Text = keyWorks ? "Covers ready" : "Key did not work";
            if (keyWorks)
            {
                await _settingsStorage.SaveAsync(_settings, cancellationToken);
            }
        });

        MessageBox.Show(
            this,
            keyWorks
                ? "SteamGridDB key works. Covers are ready."
                : "SteamGridDB key did not work. Check that you copied the full API key, then try again.",
            Text,
            MessageBoxButtons.OK,
            keyWorks ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private void RefreshHomeStatus()
    {
        if (_homeSteamStatusLabel is null || _homeSunshineStatusLabel is null || _homeCoverStatusLabel is null)
        {
            return;
        }

        _homeSteamStatusLabel.Text = File.Exists(_settings.SteamLibraryFoldersPath)
            ? "Steam ready"
            : Directory.Exists(_settings.SteamInstallPath) ? "Steam found, library list needed" : "Click Auto-detect Steam";

        if (File.Exists(_settings.SunshineAppsJsonPath))
        {
            _homeSunshineStatusLabel.Text = "Sunshine app list found";
        }
        else
        {
            var appsFolder = Path.GetDirectoryName(_settings.SunshineAppsJsonPath);
            _homeSunshineStatusLabel.Text = !string.IsNullOrWhiteSpace(appsFolder) && Directory.Exists(appsFolder)
                ? "Sunshine app list will be created"
                : "Check Sunshine app list path";
        }

        if (string.IsNullOrWhiteSpace(_settings.SteamGridDbApiKey))
        {
            _homeCoverStatusLabel.Text = "Key needed";
        }
        else if (_homeCoverStatusLabel.Text != "Covers ready" && _homeCoverStatusLabel.Text != "Key did not work")
        {
            _homeCoverStatusLabel.Text = "Key saved";
        }

        if (_homeScanStatusLabel is not null)
        {
            var selectable = _steamRows.Count(row => row.SunshineStatus is "New" or "Needs Repair");
            _homeScanStatusLabel.Text = _steamRows.Count == 0
                ? "No scan yet"
                : $"Found {_steamRows.Count} game(s), {selectable} ready to add or fix";
        }

        if (_homeApplyStatusLabel is not null)
        {
            var selected = _steamRows.Count(row => row.Import);
            _homeApplyStatusLabel.Text = selected == 0
                ? "A backup will be made before changes are saved."
                : $"{selected} selected. A backup will be made first.";
        }
    }

    private void RefreshAllGameGrids()
    {
        _steamGrid?.Refresh();
        _homeSteamGrid?.Refresh();
        RefreshHomeStatus();
    }

    private async Task AutoDetectSteamAsync()
    {
        await RunOperationAsync("Auto-detecting Steam...", async cancellationToken =>
        {
            var installPath = _steamLocator.DetectSteamInstallPath();
            var libraryFolders = _steamLocator.DetectLibraryFoldersPath();

            if (!string.IsNullOrWhiteSpace(installPath))
            {
                _steamInstallPathTextBox.Text = installPath;
            }

            if (!string.IsNullOrWhiteSpace(libraryFolders))
            {
                _libraryFoldersPathTextBox.Text = libraryFolders;
            }

            await _logger.LogAsync(
                string.IsNullOrWhiteSpace(installPath)
                    ? "Steam auto-detect did not find an install path."
                    : $"Steam auto-detected at {installPath}",
                cancellationToken);

            if (string.IsNullOrWhiteSpace(installPath))
            {
                MessageBox.Show(this, "Steam was not found in the default paths or registry.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        });
    }

    private async Task SaveSettingsAsync()
    {
        CaptureSettingsFromUi();
        await _settingsStorage.SaveAsync(_settings);
        await _logger.LogAsync("Settings saved.");
        RefreshBackupList();
        MessageBox.Show(this, "Settings saved.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task<bool> ValidateSettingsAsync(bool showMessage)
    {
        CaptureSettingsFromUi();
        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(_settings.SteamInstallPath) || !Directory.Exists(_settings.SteamInstallPath))
        {
            messages.Add("Steam install path was not found.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SteamLibraryFoldersPath) || !File.Exists(_settings.SteamLibraryFoldersPath))
        {
            messages.Add("Steam libraryfolders.vdf was not found.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SunshineAppsJsonPath))
        {
            messages.Add("Sunshine apps.json path is empty.");
        }
        else
        {
            var appsFolder = Path.GetDirectoryName(_settings.SunshineAppsJsonPath);
            if (!string.IsNullOrWhiteSpace(appsFolder) && !Directory.Exists(appsFolder))
            {
                messages.Add("Sunshine config folder was not found.");
            }
        }

        if (string.IsNullOrWhiteSpace(_settings.CoverOutputFolder))
        {
            messages.Add("Cover output folder is empty.");
        }

        var valid = messages.Count == 0;
        await _logger.LogAsync(valid ? "Settings validation passed." : "Settings validation failed: " + string.Join(" ", messages));

        if (showMessage)
        {
            MessageBox.Show(
                this,
                valid ? "Settings look valid." : string.Join(Environment.NewLine, messages),
                Text,
                MessageBoxButtons.OK,
                valid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        return valid;
    }

    private async Task ScanSteamGamesAsync()
    {
        CaptureSettingsFromUi();
        await RunOperationAsync("Scanning Steam games...", async cancellationToken =>
        {
            var scanner = new SteamScanner(_logger);
            var games = await scanner.ScanAsync(_settings.SteamLibraryFoldersPath, cancellationToken);
            var managed = await _managedStateStorage.LoadAsync(cancellationToken);
            SunshineAppDocument? document = null;

            if (!string.IsNullOrWhiteSpace(_settings.SunshineAppsJsonPath))
            {
                document = await _sunshineAppService.LoadOrCreateAsync(_settings.SunshineAppsJsonPath, cancellationToken);
            }

            _steamRows.Clear();
            foreach (var game in games)
            {
                var row = SteamGameRow.FromGame(game);
                row.CoverStatus = File.Exists(SunshineAppService.GetExpectedCoverPath(_settings.CoverOutputFolder, game.AppId))
                    ? "Downloaded"
                    : "Missing";

                if (document is null)
                {
                    row.SunshineStatus = "Ignored";
                }
                else
                {
                    var match = _sunshineAppService.FindMatchingApp(document.Apps, game.AppId, managed);
                    row.SunshineStatus = match is null
                        ? "New"
                        : _sunshineAppService.NeedsRepair(match.Value.App, game, _settings) ? "Needs Repair" : "Existing";
                }

                row.Import = row.SunshineStatus is "New" or "Needs Repair";
                _steamRows.Add(row);
            }

            await _logger.LogAsync($"Steam scan found {_steamRows.Count} installed game(s).", cancellationToken);
        });
    }

    private async Task DownloadCoversForCheckedRowsAsync()
    {
        CaptureSettingsFromUi();
        var selected = GetCheckedSteamRows().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Select one or more Steam games first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await RunOperationAsync("Downloading covers...", async cancellationToken =>
        {
            var client = new SteamGridDbClient(_logger);
            foreach (var row in selected)
            {
                var path = await client.DownloadCoverAsync(
                    row.AppId,
                    row.Name,
                    _settings.SteamGridDbApiKey,
                    _settings.CoverOutputFolder,
                    _settings.AdvancedOptions.RefreshExistingCoverArt,
                    cancellationToken);
                row.CoverStatus = string.IsNullOrWhiteSpace(path) ? "Missing" : "Downloaded";
            }

            RefreshAllGameGrids();
        });
    }

    private async Task PreviewCheckedRowsAsync()
    {
        CaptureSettingsFromUi();
        var selected = GetCheckedSteamRows().Select(row => row.ToSteamGame()).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Select one or more Steam games first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var plan = await BuildPreviewPlanAsync(selected);
        ShowPreview(plan, confirm: false);
    }

    private async Task ApplyCheckedSteamRowsAsync(bool forceRepair)
    {
        CaptureSettingsFromUi();
        var selectedRows = GetCheckedSteamRows().ToList();
        if (selectedRows.Count == 0)
        {
            MessageBox.Show(this, "Select one or more Steam games first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedGames = selectedRows.Select(row => row.ToSteamGame()).ToList();
        var plan = await BuildPreviewPlanAsync(selectedGames);
        if (!ShowPreview(plan, confirm: true))
        {
            return;
        }

        if (_settings.AdvancedOptions.RemoveStaleManagedApps && plan.StaleManagedApps.Count > 0)
        {
            var staleConfirm = MessageBox.Show(
                this,
                "Remove stale managed Steam apps listed in the preview?",
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (staleConfirm != DialogResult.Yes)
            {
                _settings.AdvancedOptions.RemoveStaleManagedApps = false;
            }
        }

        await RunOperationAsync("Applying Sunshine apps.json changes...", async cancellationToken =>
        {
            var document = await _sunshineAppService.LoadOrCreateAsync(_settings.SunshineAppsJsonPath, cancellationToken);
            var managed = await _managedStateStorage.LoadAsync(cancellationToken);
            var coverClient = new SteamGridDbClient(_logger);

            if (File.Exists(_settings.SunshineAppsJsonPath))
            {
                var backupPath = await _backupService.CreateBackupAsync(_settings.SunshineAppsJsonPath, plan.ProposedBackupPath, cancellationToken);
                await _logger.LogAsync($"Backup created: {backupPath}", cancellationToken);
            }

            foreach (var game in selectedGames)
            {
                var downloadedPath = await coverClient.DownloadCoverAsync(
                    game.AppId,
                    game.Name,
                    _settings.SteamGridDbApiKey,
                    _settings.CoverOutputFolder,
                    _settings.AdvancedOptions.RefreshExistingCoverArt,
                    cancellationToken);

                var coverPath = !string.IsNullOrWhiteSpace(downloadedPath) && File.Exists(downloadedPath)
                    ? downloadedPath
                    : "";
                var match = _sunshineAppService.FindMatchingApp(document.Apps, game.AppId, managed);

                var actualName = game.Name;
                if (match is null)
                {
                    var generatedApp = _sunshineAppService.CreateGeneratedSteamApp(game, coverPath);
                    document.Apps.Add(generatedApp);
                    actualName = SunshineAppService.GetString(generatedApp, "name") ?? game.Name;
                    await _logger.LogAsync($"Added Steam game to Sunshine: {game.Name} ({game.AppId})", cancellationToken);
                }
                else if (forceRepair || _sunshineAppService.NeedsRepair(match.Value.App, game, _settings) || selectedRows.Any(row => row.AppId == game.AppId))
                {
                    _sunshineAppService.ApplyGeneratedSteamFields(
                        match.Value.App,
                        game,
                        coverPath,
                        _settings.AdvancedOptions.PreserveManualSunshineAppNames);
                    actualName = SunshineAppService.GetString(match.Value.App, "name") ?? game.Name;
                    await _logger.LogAsync($"Updated Steam game in Sunshine: {game.Name} ({game.AppId})", cancellationToken);
                }
                else
                {
                    actualName = SunshineAppService.GetString(match.Value.App, "name") ?? game.Name;
                }

                managed.Upsert(game.AppId, actualName, coverPath, game.SteamUri);
            }

            if (_settings.AdvancedOptions.RemoveStaleManagedApps)
            {
                var removed = _sunshineAppService.RemoveStaleManagedApps(document, managed, _steamRows.Select(row => row.ToSteamGame()).ToList());
                foreach (var app in removed)
                {
                    await _logger.LogAsync($"Removed stale managed Steam app: {app}", cancellationToken);
                }
            }

            await _sunshineAppService.WriteAsync(document, cancellationToken);
            await _managedStateStorage.SaveAsync(managed, cancellationToken);
            await _logger.LogAsync("Sunshine apps.json was written.", cancellationToken);

            RefreshBackupList();
            await RefreshSteamStatusesAsync(cancellationToken);
            await RefreshSunshineAppsGridAsync(cancellationToken);

            if (_settings.AdvancedOptions.RestartSunshineAfterApply)
            {
                var confirm = MessageBox.Show(
                    this,
                    "Restart Sunshine now?",
                    Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    var processService = new SunshineProcessService(_logger);
                    await processService.RestartAsync(cancellationToken);
                }
            }
        });
    }

    private async Task<PreviewPlan> BuildPreviewPlanAsync(IReadOnlyList<SteamGame> selectedGames)
    {
        var document = await _sunshineAppService.LoadOrCreateAsync(_settings.SunshineAppsJsonPath);
        var managed = await _managedStateStorage.LoadAsync();
        var backupPath = _backupService.CreateBackupPath(_settings.SunshineAppsJsonPath);
        return _sunshineAppService.BuildPreview(
            selectedGames,
            _steamRows.Select(row => row.ToSteamGame()).ToList(),
            document,
            managed,
            _settings,
            backupPath);
    }

    private bool ShowPreview(PreviewPlan plan, bool confirm)
    {
        return PreviewDialog.ShowPreview(this, plan, confirm, _settings.AdvancedOptions.RestartSunshineAfterApply);
    }

    private static string FormatPreviewList(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return "";
        }

        var visibleItems = items.Take(12).Select(item => "  " + item);
        var suffix = items.Count > 12 ? $"{Environment.NewLine}  ...and {items.Count - 12} more" : "";
        return string.Join(Environment.NewLine, visibleItems) + suffix + Environment.NewLine;
    }

    private async Task RefreshSteamStatusesAsync(CancellationToken cancellationToken)
    {
        if (_steamRows.Count == 0)
        {
            return;
        }

        var document = await _sunshineAppService.LoadOrCreateAsync(_settings.SunshineAppsJsonPath, cancellationToken);
        var managed = await _managedStateStorage.LoadAsync(cancellationToken);

        foreach (var row in _steamRows)
        {
            row.CoverStatus = File.Exists(SunshineAppService.GetExpectedCoverPath(_settings.CoverOutputFolder, row.AppId))
                ? "Downloaded"
                : "Missing";
            var match = _sunshineAppService.FindMatchingApp(document.Apps, row.AppId, managed);
            row.SunshineStatus = match is null
                ? "New"
                : _sunshineAppService.NeedsRepair(match.Value.App, row.ToSteamGame(), _settings) ? "Needs Repair" : "Existing";
            row.Import = row.SunshineStatus is "New" or "Needs Repair";
        }

        RefreshAllGameGrids();
    }

    private async Task LoadSunshineAppsGridAsync()
    {
        CaptureSettingsFromUi();
        await RunOperationAsync("Loading Sunshine apps...", async cancellationToken =>
        {
            await RefreshSunshineAppsGridAsync(cancellationToken);
        });
    }

    private async Task RefreshSunshineAppsGridAsync(CancellationToken cancellationToken)
    {
        var document = await _sunshineAppService.LoadOrCreateAsync(_settings.SunshineAppsJsonPath, cancellationToken);
        var managed = await _managedStateStorage.LoadAsync(cancellationToken);
        var rows = _sunshineAppService.BuildRows(document, managed, _settings);
        _sunshineRows.Clear();
        foreach (var row in rows)
        {
            _sunshineRows.Add(row);
        }

        await _logger.LogAsync($"Loaded {_sunshineRows.Count} Sunshine app entr{(_sunshineRows.Count == 1 ? "y" : "ies")}", cancellationToken);
    }

    private async Task RepairSelectedSunshineAppsAsync()
    {
        CaptureSettingsFromUi();
        var rows = GetSelectedSunshineRows().Where(row => row.IsSteamUriApp).ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "Select one or more Steam URI apps to repair.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Repair {rows.Count} selected Steam URI app(s)?",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await RunOperationAsync("Repairing Sunshine apps...", async cancellationToken =>
        {
            var document = await _sunshineAppService.LoadOrCreateAsync(_settings.SunshineAppsJsonPath, cancellationToken);
            var managed = await _managedStateStorage.LoadAsync(cancellationToken);
            var backupPath = await _backupService.CreateBackupAsync(_settings.SunshineAppsJsonPath, cancellationToken: cancellationToken);
            await _logger.LogAsync($"Backup created: {backupPath}", cancellationToken);

            _sunshineAppService.RepairSunshineRows(document, managed, rows, _settings);
            await _sunshineAppService.WriteAsync(document, cancellationToken);
            await _managedStateStorage.SaveAsync(managed, cancellationToken);
            await _logger.LogAsync($"Repaired {rows.Count} selected Sunshine Steam app(s).", cancellationToken);

            RefreshBackupList();
            await RefreshSunshineAppsGridAsync(cancellationToken);
            await RefreshSteamStatusesAsync(cancellationToken);
        });
    }

    private async Task RemoveSelectedManagedSunshineAppsAsync()
    {
        CaptureSettingsFromUi();
        var rows = GetSelectedSunshineRows().Where(row => row.IsManagedByThisTool).ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "Select one or more managed Steam apps to remove.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Remove {rows.Count} managed Steam app(s) from Sunshine apps.json? This will not remove unrelated apps.",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await RunOperationAsync("Removing managed Sunshine apps...", async cancellationToken =>
        {
            var document = await _sunshineAppService.LoadOrCreateAsync(_settings.SunshineAppsJsonPath, cancellationToken);
            var managed = await _managedStateStorage.LoadAsync(cancellationToken);
            var backupPath = await _backupService.CreateBackupAsync(_settings.SunshineAppsJsonPath, cancellationToken: cancellationToken);
            await _logger.LogAsync($"Backup created: {backupPath}", cancellationToken);

            _sunshineAppService.RemoveManagedAppsByIndex(document, managed, rows.Select(row => row.Index));
            await _sunshineAppService.WriteAsync(document, cancellationToken);
            await _managedStateStorage.SaveAsync(managed, cancellationToken);
            await _logger.LogAsync($"Removed {rows.Count} managed Steam app(s) from Sunshine apps.json.", cancellationToken);

            RefreshBackupList();
            await RefreshSunshineAppsGridAsync(cancellationToken);
            await RefreshSteamStatusesAsync(cancellationToken);
        });
    }

    private async Task CreateManualBackupAsync()
    {
        CaptureSettingsFromUi();
        await RunOperationAsync("Creating manual backup...", async cancellationToken =>
        {
            var backupPath = await _backupService.CreateBackupAsync(_settings.SunshineAppsJsonPath, cancellationToken: cancellationToken);
            await _logger.LogAsync($"Manual backup created: {backupPath}", cancellationToken);
            RefreshBackupList();
        });
    }

    private async Task RestoreSelectedBackupAsync()
    {
        CaptureSettingsFromUi();
        if (_backupListBox.SelectedItem is not BackupItem backup)
        {
            MessageBox.Show(this, "Select a backup first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Restore this backup over apps.json?{Environment.NewLine}{backup.FilePath}",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await RunOperationAsync("Restoring backup...", async cancellationToken =>
        {
            if (File.Exists(_settings.SunshineAppsJsonPath))
            {
                var currentBackup = await _backupService.CreateBackupAsync(_settings.SunshineAppsJsonPath, cancellationToken: cancellationToken);
                await _logger.LogAsync($"Current apps.json backed up before restore: {currentBackup}", cancellationToken);
            }

            await _backupService.RestoreAsync(_settings.SunshineAppsJsonPath, backup.FilePath, cancellationToken);
            await _logger.LogAsync($"Restored backup: {backup.FilePath}", cancellationToken);
            RefreshBackupList();
            await RefreshSunshineAppsGridAsync(cancellationToken);
        });
    }

    private void RefreshBackupList()
    {
        CaptureSettingsFromUi();
        _backupRows.Clear();
        foreach (var backup in _backupService.ListBackups(_settings.SunshineAppsJsonPath))
        {
            _backupRows.Add(backup);
        }
    }

    private void OpenLogFolder()
    {
        AppDataPaths.Ensure();
        Process.Start(new ProcessStartInfo
        {
            FileName = AppDataPaths.LogsFolder,
            UseShellExecute = true
        });
    }

    private IEnumerable<SteamGameRow> GetCheckedSteamRows()
    {
        _steamGrid.EndEdit();
        _homeSteamGrid.EndEdit();
        return _steamRows.Where(row => row.Import);
    }

    private IEnumerable<SunshineAppRow> GetSelectedSunshineRows()
    {
        return _sunshineGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<SunshineAppRow>()
            .ToList();
    }

    private void SelectRowsByStatus(params string[] statuses)
    {
        var statusSet = statuses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in _steamRows)
        {
            row.Import = statusSet.Contains(row.SunshineStatus);
        }

        RefreshAllGameGrids();
    }

    private void SelectNoSteamRows()
    {
        foreach (var row in _steamRows)
        {
            row.Import = false;
        }

        RefreshAllGameGrids();
    }

    private async Task RunOperationAsync(string status, Func<CancellationToken, Task> operation)
    {
        if (_operationCancellation is not null)
        {
            MessageBox.Show(this, "Another operation is already running.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _operationCancellation = new CancellationTokenSource();
        _statusLabel.Text = status;
        UseWaitCursor = true;

        try
        {
            await operation(_operationCancellation.Token);
            _statusLabel.Text = "Ready";
            RefreshHomeStatus();
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Cancelled";
            await _logger.LogAsync("Operation cancelled.");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Error";
            await _logger.LogAsync($"Error: {ex.Message}");
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
            _operationCancellation.Dispose();
            _operationCancellation = null;
        }
    }

    private static string? BrowseFolder(string? currentPath)
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = Directory.Exists(currentPath) ? currentPath : "",
            UseDescriptionForTitle = true,
            Description = "Select folder"
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }

    private static string? BrowseFile(string? currentPath)
    {
        using var dialog = new OpenFileDialog
        {
            FileName = File.Exists(currentPath) ? currentPath : "",
            CheckFileExists = true,
            Filter = "All files (*.*)|*.*"
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    private static string? BrowseFileOrNewJson(string? currentPath)
    {
        using var dialog = new SaveFileDialog
        {
            FileName = string.IsNullOrWhiteSpace(currentPath) ? "apps.json" : currentPath,
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            OverwritePrompt = false
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }
}
