import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.mckay.gymoccupancy',
  appName: 'Gym Occupancy',
  webDir: 'www',
  server: {
    // url: 'https://DEPLOYED_SITE_URL_HERE',
    cleartext: false
  }
};

export default config;