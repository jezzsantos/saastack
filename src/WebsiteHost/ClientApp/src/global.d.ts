declare global {
  interface Window {
    isTestingOnly: boolean;
    isHostedOn: string;
  }
}

export {};
