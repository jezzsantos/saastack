import { Recorder, SeverityLevel } from "../recorder";
import { recordCrash, recordPageView, recordTrace, recordUse } from "../api/websiteHost";

export abstract class BrowserRecorder implements Recorder {
  crash(error: Error, message?: string): void {
    if (window.isTestingOnly) {
      console.error(error, `SaaStack: Crash: ${message}: ${error.message}, ${error.stack}`);
    }
    recordCrash({
      body: {
        message: `SaaStack: Crash: ${message}: ${error.message}, ${error.stack}`
      }
    });
  }

  trace(message: string, severityLevel: SeverityLevel): void {
    if (window.isTestingOnly) {
      console.log(`SaaStack: Trace:${severityLevel}: ${message}`);
    }
    recordTrace({
      body: {
        arguments: [],
        level: severityLevel.toString(),
        messageTemplate: message
      }
    });
  }

  traceDebug(message: string): void {
    this.trace(message, SeverityLevel.Debug);
  }

  traceInformation(message: string): void {
    this.trace(message, SeverityLevel.Information);
  }

  trackPageView(path: string): void {
    recordPageView({
      body: {
        path
      }
    });

    if (window.isTestingOnly) {
      console.log(`SaaStack: PageView: ${path}`);
    }
  }

  trackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    recordUse({
      body: {
        eventName,
        additional
      }
    });

    if (window.isTestingOnly) {
      console.log(`SaaStack: Track:${eventName}, with: ${additional}`);
    }
  }
}
