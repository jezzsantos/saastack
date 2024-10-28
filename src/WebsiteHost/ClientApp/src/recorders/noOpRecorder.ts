import { Recorder, SeverityLevel } from "../recorder";

export class NoOpRecorder implements Recorder {
  crash(error: Error, message?: string): void {
    // Does nothing by definition
  }

  trace(message: string, severityLevel: SeverityLevel): void {
    // Does nothing by definition
  }

  traceDebug(message: string): void {
    // Does nothing by definition
  }

  traceInformation(message: string): void {
    // Does nothing by definition
  }

  trackPageView(path: string): void {
    // Does nothing by definition
  }

  trackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    // Does nothing by definition
  }
}
