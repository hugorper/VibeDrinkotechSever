using VibeDrinkotechSever.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using LogStringTestApp;
using System.Timers;

namespace VibeDrinkotechSever {

    public delegate void Log(string type, string title);

	public partial class MainForm : Form {

		// Constants
		private const string SettingsFieldRunAtStartup = "RunAtStartup";
		private const string RegistryKeyId = "VibeDrinkotechSever";					// Registry app key for when it's running at startup
		private const string ConfigFile = "VibeDrinkotechSever.cfg";

		private const string LineDivider = "\t";
		private const string LineEnd = "\r\n";
		private const string DateTimeFormat = "yyyy-dd-M--HH-mm-ss";								

		// Properties
		private System.Timers.Timer _timerCheck;
		private ContextMenu _contextMenu;
		private MenuItem _menuItemOpen;
		private MenuItem _menuItemOpenConfig;
	    private MenuItem _menuItemClearLog;
		private MenuItem _menuItemStartStop;
		private MenuItem _menuItemRunAtStartup;
		private MenuItem _menuItemExit;
		private bool _allowClose;
		private bool _allowShow;
		private bool _isRunning;
		private bool _hasInitialized;
		private DateTime _lastTimeQueueWritten;
		private List<string> _queuedLogMessages;

	    private string _configSpoolPath;
	    private bool _configModeIsCredit;
		private float? _configTimeInterval;										// In millisecons
		private bool _configIsDebug;
		private string _configComPort;

	    const string LogName = "Test";
	    private LogString _myLogger = LogString.GetLogString(LogName);


		private StringBuilder _lineToLog;											// Temp, used to create the line

		// ================================================================================================================
		// CONSTRUCTOR ----------------------------------------------------------------------------------------------------

		public MainForm() {
		    // Add update callback delegate
		    _myLogger.OnLogUpdate += new LogString.LogUpdateDelegate(this.LogUpdate);

            // Setup log delegate
		    HartNineSix.Instance.Logger = delegate(string type, string title)
		    {
                LogLine(type, title);
		    };
		    
		    _myLogger.Timestamp = false;
		    _myLogger.LineTerminate = false;

			InitializeComponent();
			InitializeForm();

		    txtLog.ScrollBars = ScrollBars.Both; // use scroll bars; no text wrapping
		    txtLog.MaxLength = _myLogger.MaxChars + 100;
		}

	    // Updates that come from a different thread can not directly change the
	    // TextBox component. This must be done through Invoke().
	    private delegate void UpdateDelegate();
	    private void LogUpdate()
	    {
	        Invoke(new UpdateDelegate(
	            delegate
	            {
	                txtLog.Text = _myLogger.Log;
	            })
	        );
	    }

		// ================================================================================================================
		// EVENT INTERFACE ------------------------------------------------------------------------------------------------

		private void OnFormLoad(object sender, EventArgs e) {
			// First time the form is shown
		}

		protected override void SetVisibleCore(bool isVisible) {
			if (!_allowShow) {
				// Initialization form show, when it's ran: doesn't allow showing form
				isVisible = false;
				if (!this.IsHandleCreated) CreateHandle();
			}
			base.SetVisibleCore(isVisible);
		}

		private void OnFormClosing(object sender, FormClosingEventArgs e) {
			// Form is attempting to close
			if (!_allowClose) {
				// User initiated, just minimize instead
				e.Cancel = true;
				Hide();
			}

		    _myLogger.OnLogUpdate -= new LogString.LogUpdateDelegate(this.LogUpdate);
		}

		private void OnFormClosed(object sender, FormClosedEventArgs e) {
			// Stops everything
			Stop();

			// If debugging, un-hook itself from startup
			if (System.Diagnostics.Debugger.IsAttached && WindowsRunAtStartup) WindowsRunAtStartup = false;
		}

		private void OnTimer(Object source, ElapsedEventArgs e)
		{
			// Timer tick: check for the current application
		    if (_configModeIsCredit)
		    {
		        HartNineSix.Instance.RequestToSend();
		    }
		    else
		    {
		        HartNineSix.Instance.RequestToReceive();
		    }

			// Write to log if enough time passed
			if (_queuedLogMessages.Count > 0 ) {
				CommitLines();
			}

		    if (_timerCheck != null)
		    {
                 _timerCheck.Start();
		    }
		}

		private void OnResize(object sender, EventArgs e) {
			// Resized window
			//notifyIcon.BalloonTipTitle = "Minimize to Tray App";
			//notifyIcon.BalloonTipText = "You have successfully minimized your form.";

			if (WindowState == FormWindowState.Minimized) {
				//notifyIcon.ShowBalloonTip(500);
				this.Hide();
			}
		}
	    
		private void OnMenuItemOpenClicked(object sender, EventArgs e) {
			ShowForm();
		}

	    private void OnMenuItemOpenConfigClicked(object sender, EventArgs e) {
	        Process.Start("notepad.exe", ConfigFile);
	    }

	    private void OnMenuItemOpenClearLogClicked(object sender, EventArgs e) {
	        _myLogger.Clear();
	    }

		private void OnMenuItemStartStopClicked(object sender, EventArgs e) {
			if (_isRunning) {
				Stop();
			} else {
				Start();
			}
		}

		private void OnMenuItemRunAtStartupClicked(object sender, EventArgs e) {
			_menuItemRunAtStartup.Checked = !_menuItemRunAtStartup.Checked;
			SettingsRunAtStartup = _menuItemRunAtStartup.Checked;
			ApplySettingsRunAtStartup();
		}

		private void OnMenuItemExitClicked(object sender, EventArgs e) {
			Exit();
		}

		private void OnDoubleClickNotificationIcon(object sender, MouseEventArgs e) {
			ShowForm();
		}


		// ================================================================================================================
		// INTERNAL INTERFACE ---------------------------------------------------------------------------------------------

		private void InitializeForm() {
			// Initialize

			if (!_hasInitialized) {
				_allowClose = false;
				_isRunning = false;
				_queuedLogMessages = new List<string>();
				_lineToLog = new StringBuilder();
				_allowShow = false;

				// Force working folder
				System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

				// Read configuration
				ReadConfiguration();

			    HartNineSix.Instance.SpoolPath = _configSpoolPath;
			    HartNineSix.Instance.WaitTime = (int)(_configTimeInterval * 1f);
			    HartNineSix.Instance.IsDebug = _configIsDebug;

				// Create context menu for the tray icon and update it
				CreateContextMenu();

				// Update tray
				UpdateTrayIcon();

				// Check if it needs to run at startup
				ApplySettingsRunAtStartup();

				// Finally, start
				Start();

				_hasInitialized = true;
			}
		}

		private void CreateContextMenu() {
			// Initialize context menu
			_contextMenu = new ContextMenu();

			// Initialize menu items
			_menuItemOpen = new MenuItem();
			_menuItemOpen.Index = 0;
			_menuItemOpen.Text = "&Open";
			_menuItemOpen.Click += new EventHandler(OnMenuItemOpenClicked);
			_contextMenu.MenuItems.Add(_menuItemOpen);
		    
		    _contextMenu.MenuItems.Add("-");

		    _menuItemOpenConfig = new MenuItem();
		    _menuItemOpenConfig.Index = 0;
		    _menuItemOpenConfig.Text = "Open &Config file";
		    _menuItemOpenConfig.Click += new EventHandler(OnMenuItemOpenConfigClicked);
		    _contextMenu.MenuItems.Add(_menuItemOpenConfig);

		    _menuItemClearLog = new MenuItem();
		    _menuItemClearLog.Index = 0;
		    _menuItemClearLog.Text = "Clear &Log";
		    _menuItemClearLog.Click += new EventHandler(OnMenuItemOpenClearLogClicked);
		    _contextMenu.MenuItems.Add(_menuItemClearLog);


			_menuItemStartStop = new MenuItem();
			_menuItemStartStop.Index = 0;
			_menuItemStartStop.Text = ""; // Set later
			_menuItemStartStop.Click += new EventHandler(OnMenuItemStartStopClicked);
			_contextMenu.MenuItems.Add(_menuItemStartStop);

			_contextMenu.MenuItems.Add("-");

			_menuItemRunAtStartup = new MenuItem();
			_menuItemRunAtStartup.Index = 0;
			_menuItemRunAtStartup.Text = "Run at Windows startup";
			_menuItemRunAtStartup.Click += new EventHandler(OnMenuItemRunAtStartupClicked);
			_menuItemRunAtStartup.Checked = SettingsRunAtStartup;
			_contextMenu.MenuItems.Add(_menuItemRunAtStartup);

			_contextMenu.MenuItems.Add("-");

			_menuItemExit = new MenuItem();
			_menuItemExit.Index = 1;
			_menuItemExit.Text = "E&xit";
			_menuItemExit.Click += new EventHandler(OnMenuItemExitClicked);
			_contextMenu.MenuItems.Add(_menuItemExit);

			notifyIcon.ContextMenu = _contextMenu;

			UpdateContextMenu();
		}

		private void UpdateContextMenu() {
			// Update start/stop command
			if (_menuItemStartStop != null) {
				if (_isRunning) {
					_menuItemStartStop.Text = "&Stop";
				} else {
					_menuItemStartStop.Text = "&Start";
				}
			}
		}

		private void UpdateTrayIcon() {
			if (_isRunning) {
				notifyIcon.Icon = VibeDrinkotechSever.Properties.Resources.iconNormal;
				notifyIcon.Text = "Le Vibe Drinkotech Server (started)";
			} else {
				notifyIcon.Icon = VibeDrinkotechSever.Properties.Resources.iconStopped;
				notifyIcon.Text = "Le Vibe Drinkotech Server (stopped)";
			}
		}

		private void ReadConfiguration() {
			// Read the current configuration file

			// Read default file
			ConfigParser configDefault = new ConfigParser(VibeDrinkotechSever.Properties.Resources.default_config);
			ConfigParser configUser;

			if (!System.IO.File.Exists(ConfigFile)) {
				// Config file not found, create it first
				Console.Write("Config file does not exist, creating");

				// Write file so it can be edited by the user
				System.IO.File.WriteAllText(ConfigFile, VibeDrinkotechSever.Properties.Resources.default_config);

				// User config is the same as the default
				configUser = configDefault;
			} else {
				// Read the existing user config
				configUser = new ConfigParser(System.IO.File.ReadAllText(ConfigFile));
			}

			// Interprets config data
	        _configModeIsCredit = (configUser.getString("mode") ?? configDefault.getString("mode")) == "credit";
		    _configSpoolPath = configUser.getString("spool") ?? configDefault.getString("spool");
			_configTimeInterval = configUser.getFloat("timer") ?? configDefault.getFloat("timer");
			_configComPort = configUser.getString("comPort") ?? configDefault.getString("comPort");
			_configIsDebug = Boolean.Parse(configUser.getString("isDebug") ?? configDefault.getString("isDebug"));

		    if (_configIsDebug) LogLine("read::config", "Is credit mode = " + _configModeIsCredit.ToString());
		    if (_configIsDebug) LogLine("read::config", "Spool path = " + _configSpoolPath);
		    if (_configIsDebug) LogLine("read::config", "Time interval = " + _configTimeInterval);
		    if (_configIsDebug) LogLine("read::config", "Com port = " + _configComPort);
		}

		private void Start() {
			if (!_isRunning)
			{
			    LogStart();
			    if (!HartNineSix.Instance.OpenComPort(_configComPort))
			    {
                    LogLine("error:app", "Could not finalize app start");
			        return;
			    }

				// Initialize timer
				_timerCheck = new System.Timers.Timer((int)(_configTimeInterval * 1f));
			    _timerCheck.Elapsed += OnTimer;
			    _timerCheck.AutoReset = false;
				_timerCheck.Start();

				_lastTimeQueueWritten = DateTime.Now;
				_isRunning = true;

				UpdateContextMenu();
				UpdateTrayIcon();
			}
		}

		private void Stop() {
		    LogString.PersistAll();

			if (_isRunning) {
				LogStop();

				_timerCheck.Stop();
			    _timerCheck.Enabled = false;
				_timerCheck.Dispose();
				_timerCheck = null;

			    HartNineSix.Instance.CloseComPort();

				_isRunning = false;

				UpdateContextMenu();
				UpdateTrayIcon();
			}
		}

	    private void LogStart() {
	        // Log stopping the application
	        LogLine("status::start");
	    }

		private void LogStop() {
			// Log stopping the application
			LogLine("status::stop");
		}

	    private void LogLine(string type, string title = "", string location = "", string subject = "") {
			// Log a single line
			DateTime now = DateTime.Now;

			_lineToLog.Clear();
			_lineToLog.Append(now.ToString(DateTimeFormat));
			_lineToLog.Append(LineDivider);
			_lineToLog.Append(type);
			_lineToLog.Append(LineDivider);
			_lineToLog.Append(Environment.MachineName);
			_lineToLog.Append(LineDivider);
			_lineToLog.Append(title);
			_lineToLog.Append(LineDivider);
			_lineToLog.Append(location);
			_lineToLog.Append(LineDivider);
			_lineToLog.Append(subject);
			_lineToLog.Append(LineEnd);

			_queuedLogMessages.Add(_lineToLog.ToString());

		    CommitLines();

		}

		private void CommitLines() {
		    try
		    {
                
		        // If no commit needed, just return
		        if (_queuedLogMessages.Count == 0) return;

		        _lineToLog.Clear();
		        foreach (var line in _queuedLogMessages) {
		            _myLogger.Add(line);
		            _lineToLog.Append(line);
		        }


		        // todo append to log
		    

		        UpdateContextMenu();

		        _queuedLogMessages.Clear();

		        _lastTimeQueueWritten = DateTime.Now;
		    }
		    catch 
		    {
		       
		    }
		}

		private void ApplySettingsRunAtStartup() {
			// Check whether it's properly set to run at startup or not
			if (SettingsRunAtStartup) {
				// Should run at startup
				if (!WindowsRunAtStartup) WindowsRunAtStartup = true;
			} else {
				// Should not run at startup
				if (WindowsRunAtStartup) WindowsRunAtStartup = false;
			}
		}

		private void ShowForm() {
			_allowShow = true;
			Show();
			WindowState = FormWindowState.Normal;
		}

		private void Exit() {
			_allowClose = true;
			Close();
		}


		// ================================================================================================================
		// ACCESSOR INTERFACE ---------------------------------------------------------------------------------------------

		private bool SettingsRunAtStartup {
			// Whether the settings say the app should run at startup or not
			get {
				return (bool)Settings.Default[SettingsFieldRunAtStartup];
			}
			set {
				Settings.Default[SettingsFieldRunAtStartup] = value;
				Settings.Default.Save();
			}
		}

		private bool WindowsRunAtStartup {
			// Whether it's actually set to run at startup or not
			get {
				return GetStartupRegistryKey().GetValue(RegistryKeyId) != null;
			}
			set {
				if (value) {
					// Add
					GetStartupRegistryKey(true).SetValue(RegistryKeyId, Application.ExecutablePath.ToString());
					//Console.WriteLine("RUN AT STARTUP SET AS => TRUE");
				} else {
					// Remove
					GetStartupRegistryKey(true).DeleteValue(RegistryKeyId, false);
					//Console.WriteLine("RUN AT STARTUP SET AS => FALSE");
				}
			}
		}

		private RegistryKey GetStartupRegistryKey(bool writable = false) {
			return Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable);
		}

	}
}
