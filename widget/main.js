const { app, BrowserWindow, screen, ipcMain } = require('electron');
const path = require('path');
const fs = require('fs');

const configPath = path.join(app.getPath('userData'), 'widget-config.json');
const autoStartPath = path.join(app.getPath('userData'), 'autostart.json');

function loadConfig() {
  try { return JSON.parse(fs.readFileSync(configPath, 'utf-8')); } catch { return {}; }
}

function saveConfig(data) {
  try { fs.writeFileSync(configPath, JSON.stringify(data)); } catch {}
}

function loadAutoStart() {
  try { return JSON.parse(fs.readFileSync(autoStartPath, 'utf-8')).enabled; } catch { return true; }
}

function saveAutoStart(enabled) {
  try { fs.writeFileSync(autoStartPath, JSON.stringify({ enabled })); } catch {}
}

let mainWindow;

app.setLoginItemSettings({ openAtLogin: true });

app.whenReady().then(() => {
  app.setLoginItemSettings({ openAtLogin: loadAutoStart() });

  const config = loadConfig();
  const { width: screenW } = screen.getPrimaryDisplay().workAreaSize;

  const winOptions = {
    width: 280,
    height: 380,
    frame: false,
    transparent: true,
    type: 'toolbar',
    alwaysOnTop: false,
    skipTaskbar: true,
    resizable: false,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js')
    },
    show: false
  };

  if (config.x !== undefined && config.y !== undefined) {
    winOptions.x = config.x;
    winOptions.y = config.y;
  } else {
    winOptions.x = screenW - 300;
    winOptions.y = 80;
  }

  mainWindow = new BrowserWindow(winOptions);

  const htmlPath = app.isPackaged
    ? path.join(process.resourcesPath, 'app.html')
    : path.join(__dirname, '..', '天气插件.html');

  mainWindow.loadFile(htmlPath);

  mainWindow.once('ready-to-show', () => {
    mainWindow.show();
  });

  mainWindow.on('moved', () => {
    const [x, y] = mainWindow.getPosition();
    saveConfig({ x, y });
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
});

ipcMain.handle('get-auto-start', () => {
  return app.getLoginItemSettings().openAtLogin;
});

ipcMain.handle('set-auto-start', (event, enabled) => {
  app.setLoginItemSettings({ openAtLogin: enabled });
  saveAutoStart(enabled);
  return enabled;
});

app.on('window-all-closed', () => {
  app.quit();
});