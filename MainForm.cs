using VibeDrinkotechSever.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using LogStringTestApp;

namespace VibeDrinkotechSever {

	public partial class MainForm : Form {

		// Constants
		private const string SETTINGS_FIELD_RUN_AT_STARTUP = "RunAtStartup";
		private const string REGISTRY_KEY_ID = "VibeDrinkotechSever";					// Registry app key for when it's running at startup
		private const string CONFIG_FILE = "VibeDrinkotechSever.cfg";

		private const string LINE_DIVIDER = "\t";
		private const string LINE_END = "\r\n";
		private const string DATE_TIME_FORMAT = "yyyy-dd-M--HH-mm-ss";								

		// Properties
		private Timer timerCheck;
		private ContextMenu contextMenu;
		private MenuItem menuItemOpen;
		private MenuItem menuItemOpenConfig;
	    private MenuItem menuItemClearLog;
		private MenuItem menuItemStartStop;
		private MenuItem menuItemRunAtStartup;
		private MenuItem menuItemExit;
		private bool allowClose;
		private bool allowShow;
		private bool isRunning;
		private bool isUserIdle;
		private bool hasInitialized;
		private string lastUserProcessId;
		private string lastFileNameSaved;
		private int lastDayLineLogged;
		private DateTime lastTimeQueueWritten;
		private List<string> queuedLogMessages;

	    private string configSpoolPath;
	    private bool configModeIsCredit;
		private float? configTimeInterval;										// In millisecons
		private bool? configIsDebug;
		private string configComPort;

	    const string LOG_NAME = "Test";
	    private LogString myLogger = LogString.GetLogString(LOG_NAME);


		private StringBuilder lineToLog;											// Temp, used to create the line

		// ================================================================================================================
		// CONSTRUCTOR ----------------------------------------------------------------------------------------------------

		public MainForm() {
		    // Add update callback delegate
		    myLogger.OnLogUpdate += new LogString.LogUpdateDelegate(this.LogUpdate);

		    myLogger.Timestamp = false;
		    myLogger.LineTerminate = false;

			InitializeComponent();
			initializeForm();

		    txtLog.ScrollBars = ScrollBars.Both; // use scroll bars; no text wrapping
		    txtLog.MaxLength = myLogger.MaxChars + 100;
		}

	    // Updates that come from a different thread can not directly change the
	    // TextBox component. This must be done through Invoke().
	    private delegate void UpdateDelegate();
	    private void LogUpdate()
	    {
	        Invoke(new UpdateDelegate(
	            delegate
	            {
	                txtLog.Text = myLogger.Log;
	            })
	        );
	    }

		// ================================================================================================================
		// EVENT INTERFACE ------------------------------------------------------------------------------------------------

		private void onFormLoad(object sender, EventArgs e) {
			// First time the form is shown
		}

		protected override void SetVisibleCore(bool isVisible) {
			if (!allowShow) {
				// Initialization form show, when it's ran: doesn't allow showing form
				isVisible = false;
				if (!this.IsHandleCreated) CreateHandle();
			}
			base.SetVisibleCore(isVisible);
		}

		private void onFormClosing(object sender, FormClosingEventArgs e) {
			// Form is attempting to close
			if (!allowClose) {
				// User initiated, just minimize instead
				e.Cancel = true;
				Hide();
			}

		    myLogger.OnLogUpdate -= new LogString.LogUpdateDelegate(this.LogUpdate);
		}

		private void onFormClosed(object sender, FormClosedEventArgs e) {
			// Stops everything
			stop();

			// If debugging, un-hook itself from startup
			if (System.Diagnostics.Debugger.IsAttached && windowsRunAtStartup) windowsRunAtStartup = false;
		}

		private void onTimer(object sender, EventArgs e) {
			// Timer tick: check for the current application


			// Write to log if enough time passed
			if (queuedLogMessages.Count > 0 ) {
				commitLines();
			}
		}

		private void onResize(object sender, EventArgs e) {
			// Resized window
			//notifyIcon.BalloonTipTitle = "Minimize to Tray App";
			//notifyIcon.BalloonTipText = "You have successfully minimized your form.";

			if (WindowState == FormWindowState.Minimized) {
				//notifyIcon.ShowBalloonTip(500);
				this.Hide();
			}
		}
	    
		private void onMenuItemOpenClicked(object Sender, EventArgs e) {
			showForm();
		}

	    private void onMenuItemOpenConfigClicked(object Sender, EventArgs e) {
	        Process.Start("notepad.exe", CONFIG_FILE);
	    }

	    private void onMenuItemOpenClearLogClicked(object Sender, EventArgs e) {
	        myLogger.Clear();
	    }

		private void onMenuItemStartStopClicked(object Sender, EventArgs e) {
			if (isRunning) {
				stop();
			} else {
				start();
			}
		}

		private void onMenuItemRunAtStartupClicked(object Sender, EventArgs e) {
			menuItemRunAtStartup.Checked = !menuItemRunAtStartup.Checked;
			settingsRunAtStartup = menuItemRunAtStartup.Checked;
			applySettingsRunAtStartup();
		}

		private void onMenuItemExitClicked(object Sender, EventArgs e) {
			exit();
		}

		private void onDoubleClickNotificationIcon(object sender, MouseEventArgs e) {
			showForm();
		}


		// ================================================================================================================
		// INTERNAL INTERFACE ---------------------------------------------------------------------------------------------

		private void initializeForm() {
			// Initialize

			if (!hasInitialized) {
				allowClose = false;
				isRunning = false;
				queuedLogMessages = new List<string>();
				lineToLog = new StringBuilder();
				lastFileNameSaved = "";
				allowShow = false;

				// Force working folder
				System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

				// Read configuration
				readConfiguration();

				// Create context menu for the tray icon and update it
				createContextMenu();

				// Update tray
				updateTrayIcon();

				// Check if it needs to run at startup
				applySettingsRunAtStartup();

				// Finally, start
				start();

				hasInitialized = true;
			}
		}

		private void createContextMenu() {
			// Initialize context menu
			contextMenu = new ContextMenu();

			// Initialize menu items
			menuItemOpen = new MenuItem();
			menuItemOpen.Index = 0;
			menuItemOpen.Text = "&Open";
			menuItemOpen.Click += new EventHandler(onMenuItemOpenClicked);
			contextMenu.MenuItems.Add(menuItemOpen);
		    
		    contextMenu.MenuItems.Add("-");

		    menuItemOpenConfig = new MenuItem();
		    menuItemOpenConfig.Index = 0;
		    menuItemOpenConfig.Text = "Open &Config file";
		    menuItemOpenConfig.Click += new EventHandler(onMenuItemOpenConfigClicked);
		    contextMenu.MenuItems.Add(menuItemOpenConfig);

		    menuItemClearLog = new MenuItem();
		    menuItemClearLog.Index = 0;
		    menuItemClearLog.Text = "Clear &Log";
		    menuItemClearLog.Click += new EventHandler(onMenuItemOpenClearLogClicked);
		    contextMenu.MenuItems.Add(menuItemClearLog);


			menuItemStartStop = new MenuItem();
			menuItemStartStop.Index = 0;
			menuItemStartStop.Text = ""; // Set later
			menuItemStartStop.Click += new EventHandler(onMenuItemStartStopClicked);
			contextMenu.MenuItems.Add(menuItemStartStop);

			contextMenu.MenuItems.Add("-");

			menuItemRunAtStartup = new MenuItem();
			menuItemRunAtStartup.Index = 0;
			menuItemRunAtStartup.Text = "Run at Windows startup";
			menuItemRunAtStartup.Click += new EventHandler(onMenuItemRunAtStartupClicked);
			menuItemRunAtStartup.Checked = settingsRunAtStartup;
			contextMenu.MenuItems.Add(menuItemRunAtStartup);

			contextMenu.MenuItems.Add("-");

			menuItemExit = new MenuItem();
			menuItemExit.Index = 1;
			menuItemExit.Text = "E&xit";
			menuItemExit.Click += new EventHandler(onMenuItemExitClicked);
			contextMenu.MenuItems.Add(menuItemExit);

			notifyIcon.ContextMenu = contextMenu;

			updateContextMenu();
		}

		private void updateContextMenu() {
			// Update start/stop command
			if (menuItemStartStop != null) {
				if (isRunning) {
					menuItemStartStop.Text = "&Stop";
				} else {
					menuItemStartStop.Text = "&Start";
				}
			}
		}

		private void updateTrayIcon() {
			if (isRunning) {
				notifyIcon.Icon = VibeDrinkotechSever.Properties.Resources.iconNormal;
				notifyIcon.Text = "Le Vibe Drinkotech Server (started)";
			} else {
				notifyIcon.Icon = VibeDrinkotechSever.Properties.Resources.iconStopped;
				notifyIcon.Text = "Le Vibe Drinkotech Server (stopped)";
			}
		}

		private void readConfiguration() {
			// Read the current configuration file

			// Read default file
			ConfigParser configDefault = new ConfigParser(VibeDrinkotechSever.Properties.Resources.default_config);
			ConfigParser configUser;

			if (!System.IO.File.Exists(CONFIG_FILE)) {
				// Config file not found, create it first
				Console.Write("Config file does not exist, creating");

				// Write file so it can be edited by the user
				System.IO.File.WriteAllText(CONFIG_FILE, VibeDrinkotechSever.Properties.Resources.default_config);

				// User config is the same as the default
				configUser = configDefault;
			} else {
				// Read the existing user config
				configUser = new ConfigParser(System.IO.File.ReadAllText(CONFIG_FILE));
			}

			// Interprets config data
	        configModeIsCredit = (configUser.getString("mode") ?? configDefault.getString("mode")) == "credit";
		    configSpoolPath = configUser.getString("spool") ?? configDefault.getString("spool");
			configTimeInterval = configUser.getFloat("timer") ?? configDefault.getFloat("timer");
			configComPort = configUser.getString("comPort") ?? configDefault.getString("comPort");
			configIsDebug = Boolean.Parse(configUser.getString("isDebug") ?? configDefault.getString("isDebug"));
		}

		private void start() {
			if (!isRunning) {
				// Initialize timer
				timerCheck = new Timer();
				timerCheck.Tick += new EventHandler(onTimer);
				timerCheck.Interval = (int)(configTimeInterval * 1f);
				timerCheck.Start();

				lastTimeQueueWritten = DateTime.Now;
				isRunning = true;

				updateContextMenu();
				updateTrayIcon();
			    logStart();
			}
		}

		private void stop() {
			if (isRunning) {
				logStop();

				timerCheck.Stop();
				timerCheck.Dispose();
				timerCheck = null;

				isRunning = false;

				updateContextMenu();
				updateTrayIcon();
			}
		}

	    private void logStart() {
	        // Log stopping the application
	        logLine("status::start", true);
	    }

		private void logStop() {
			// Log stopping the application
			logLine("status::stop", true);
		}

		private void logLine(string type, bool forceCommit = false, bool usePreviousDayFileName = false, float idleTimeOffsetSeconds = 0) {
			logLine(type, "", "", "", forceCommit, usePreviousDayFileName, idleTimeOffsetSeconds);
		}

		private void logLine(string type, string title, string location, string subject, bool forceCommit = false, bool usePreviousDayFileName = false, float idleTimeOffsetSeconds = 0) {
			// Log a single line
			DateTime now = DateTime.Now;

			lineToLog.Clear();
			lineToLog.Append(now.ToString(DATE_TIME_FORMAT));
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(type);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(Environment.MachineName);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(title);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(location);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(subject);
			lineToLog.Append(LINE_END);

			queuedLogMessages.Add(lineToLog.ToString());
			lastDayLineLogged = DateTime.Now.Day;

		    commitLines();

		}

		private void commitLines() {
			// Commit all currently queued lines to the file

			// If no commit needed, just return
			if (queuedLogMessages.Count == 0) return;

			lineToLog.Clear();
			foreach (var line in queuedLogMessages) {
			    myLogger.Add(line);
				lineToLog.Append(line);
			}


            // todo append to log
		    

		    updateContextMenu();

		    queuedLogMessages.Clear();

		    lastTimeQueueWritten = DateTime.Now;
		}

		private void applySettingsRunAtStartup() {
			// Check whether it's properly set to run at startup or not
			if (settingsRunAtStartup) {
				// Should run at startup
				if (!windowsRunAtStartup) windowsRunAtStartup = true;
			} else {
				// Should not run at startup
				if (windowsRunAtStartup) windowsRunAtStartup = false;
			}
		}

		private void showForm() {
			allowShow = true;
			Show();
			WindowState = FormWindowState.Normal;
		}

		private void exit() {
			allowClose = true;
			Close();
		}


		// ================================================================================================================
		// ACCESSOR INTERFACE ---------------------------------------------------------------------------------------------

		private bool settingsRunAtStartup {
			// Whether the settings say the app should run at startup or not
			get {
				return (bool)Settings.Default[SETTINGS_FIELD_RUN_AT_STARTUP];
			}
			set {
				Settings.Default[SETTINGS_FIELD_RUN_AT_STARTUP] = value;
				Settings.Default.Save();
			}
		}

		private bool windowsRunAtStartup {
			// Whether it's actually set to run at startup or not
			get {
				return getStartupRegistryKey().GetValue(REGISTRY_KEY_ID) != null;
			}
			set {
				if (value) {
					// Add
					getStartupRegistryKey(true).SetValue(REGISTRY_KEY_ID, Application.ExecutablePath.ToString());
					//Console.WriteLine("RUN AT STARTUP SET AS => TRUE");
				} else {
					// Remove
					getStartupRegistryKey(true).DeleteValue(REGISTRY_KEY_ID, false);
					//Console.WriteLine("RUN AT STARTUP SET AS => FALSE");
				}
			}
		}

		private RegistryKey getStartupRegistryKey(bool writable = false) {
			return Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable);
		}

	}
}
