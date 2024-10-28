import { SeverityLevel } from "../recorder";
import { BrowserRecorder } from "./browserRecorder";

export class AwsRecorder extends BrowserRecorder {
  crash(error: Error, message?: string): void {
    super.crash(error, message);
  }

  trace(message: string, severityLevel: SeverityLevel): void {
    super.trace(message, severityLevel);
  }

  traceDebug(message: string): void {
    super.traceDebug(message);
  }

  traceInformation(message: string): void {
    super.traceInformation(message);
  }

  trackPageView(path: string): void {
    super.trackPageView(path);
  }

  trackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    super.trackUsage(eventName, additional);
  }
}
