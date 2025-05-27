import { AzureRecorder } from "./recorders/azureRecorder";
import { AwsRecorder } from "./recorders/awsRecorder";
import { NoOpRecorder } from "./recorders/noOpRecorder";

export interface Recorder {
  crash: (error: Error, message?: string) => void;
  trace: (message: string, severityLevel: SeverityLevel) => void;
  traceDebug: (message: string) => void;
  traceInformation: (message: string) => void;
  trackPageView: (path: string) => void;
  trackUsage: (eventName: string, additional?: { [index: string]: any }) => void;
}

export const enum SeverityLevel {
  Debug = "Debug",
  Information = "Information",
  Warning = "Warning",
  Error = "Error"
}

class LazyLoadingRecorder implements Recorder {
  private recorder: Recorder | undefined = undefined;

  crash(error: Error, message?: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.crash(error, message);
  }

  trace(message: string, severityLevel: SeverityLevel): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.trace(message, severityLevel);
  }

  traceDebug(message: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.traceDebug(message);
  }

  traceInformation(message: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.traceInformation(message);
  }

  trackPageView(path: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.trackPageView(path);
  }

  trackUsage(eventName: string, additional?: { [val: string]: any } | undefined): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.trackUsage(eventName, additional);
  }

  private ensureUnderlyingRecorder() {
    if (this.recorder !== undefined) {
      return;
    }

    if (window.isHostedOn === "AZURE") {
      this.recorder = new AzureRecorder();
      return;
    }

    if (window.isHostedOn === "AWS") {
      this.recorder = new AwsRecorder();
      return;
    }

    this.recorder = new NoOpRecorder();
  }
}

export const recorder = new LazyLoadingRecorder();
