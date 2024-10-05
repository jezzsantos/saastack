import { Recorder, SeverityLevel } from "../recorder";

export class NoOpRecorder implements Recorder {
  Crash(error: Error, message?: string): void {
    // Does nothing by definition
  }

  Trace(message: string, severityLevel: SeverityLevel): void {
    // Does nothing by definition
  }

  TraceDebug(message: string): void {
    // Does nothing by definition
  }

  TraceInformation(message: string): void {
    // Does nothing by definition
  }

  TrackPageView(path: string): void {
    // Does nothing by definition
  }

  TrackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    // Does nothing by definition
  }
}
