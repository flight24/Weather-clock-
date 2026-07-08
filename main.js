const { app, BrowserWindow, screen } = require('electron');
const path = require('path');
const fs = require('fs');

const configPath = path.join(app.getPath('userData'), 'widget-config.json');

function loadConfig() {
  try {
    return JSON.parse(fs.readFileSync(configPath, 'utf-8'));
  } catch {
    return {};
  }
}

function saveConfig(data) {
  try {
    fs.writeFileSync(configPath, JSON.stringify(data));
  } catch {}
}

let mainWindow;

app.whenReady().then(() => {
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
      contextIsolation: true
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

app.on('window-all-closed', () => {
  app.quit();
});
