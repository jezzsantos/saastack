import { SeverityLevel } from "../recorder";
import { BrowserRecorder } from "./browserRecorder";

export class AwsRecorder extends BrowserRecorder {
  Crash(error: Error, message?: string): void {
    super.Crash(error, message);
  }

  Trace(message: string, severityLevel: SeverityLevel): void {
    super.Trace(message, severityLevel);
  }

  TraceDebug(message: string): void {
    super.TraceDebug(message);
  }

  TraceInformation(message: string): void {
    super.TraceInformation(message);
  }

  TrackPageView(path: string): void {
    super.TrackPageView(path);
  }

  TrackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    super.TrackUsage(eventName, additional);
  }
}
