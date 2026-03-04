import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.mckay.gymoccupancy',
  appName: 'Gym Occupancy',
  webDir: 'www',
  server: {
    url: 'http://10.0.2.2:5500',
    cleartext: true
  }
};

export default config;