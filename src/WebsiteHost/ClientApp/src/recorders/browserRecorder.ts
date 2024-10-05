import { Recorder, SeverityLevel } from "../recorder";
import { recordCrash, recordPageView, recordTrace, recordUse } from "../api/websiteHost";

export abstract class BrowserRecorder implements Recorder {
  Crash(error: Error, message?: string): void {
    if (window.isTestingOnly) {
      console.error(error, `SaaStack: Crash: ${message}: ${error.message}, ${error.stack}`);
    }
    recordCrash({
      body: {
        message: `SaaStack: Crash: ${message}: ${error.message}, ${error.stack}`
      }
    });
  }

  Trace(message: string, severityLevel: SeverityLevel): void {
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

  TraceDebug(message: string): void {
    this.Trace(message, SeverityLevel.Debug);
  }

  TraceInformation(message: string): void {
    this.Trace(message, SeverityLevel.Information);
  }

  TrackPageView(path: string): void {
    recordPageView({
      body: {
        path
      }
    });

    if (window.isTestingOnly) {
      console.log(`SaaStack: PageView: ${path}`);
    }
  }

  TrackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
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
