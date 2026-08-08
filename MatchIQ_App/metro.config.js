const { getDefaultConfig } = require('expo/metro-config');

const config = getDefaultConfig(__dirname);

// Windows Metro FallbackWatcher crashes (ENOENT) when transient build folders
// appear/disappear under unity exports or native .cxx CMake temps.
const blockPatterns = [
  /[\\/]unity[\\/].*/,
  /[\\/]android[\\/]\.cxx[\\/].*/,
  /[\\/]android[\\/]app[\\/]build[\\/].*/,
  /[\\/]node_modules[\\/].*[\\/]android[\\/]\.cxx[\\/].*/,
  /[\\/]CMakeFiles[\\/].*/,
  /[\\/]CMakeTmp[\\/].*/,
];

const existing = config.resolver.blockList;
config.resolver.blockList = [
  ...(Array.isArray(existing) ? existing : existing ? [existing] : []),
  ...blockPatterns,
];

module.exports = config;
