import { AzureRecorder } from "./recorders/azureRecorder";
import { AwsRecorder } from "./recorders/awsRecorder";
import { NoOpRecorder } from "./recorders/noOpRecorder";

export interface Recorder {
  Crash: (error: Error, message?: string) => void;
  Trace: (message: string, severityLevel: SeverityLevel) => void;
  TraceDebug: (message: string) => void;
  TraceInformation: (message: string) => void;
  TrackPageView: (path: string) => void;
  TrackUsage: (eventName: string, additional?: { [index: string]: any }) => void;
}

export const enum SeverityLevel {
  Debug = "Debug",
  Information = "Information",
  Warning = "Warning",
  Error = "Error"
}

class LazyLoadingRecorder implements Recorder {
  private recorder: Recorder | undefined = undefined;

  Crash(error: Error, message?: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.Crash(error, message);
  }

  Trace(message: string, severityLevel: SeverityLevel): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.Trace(message, severityLevel);
  }

  TraceDebug(message: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.TraceDebug(message);
  }

  TraceInformation(message: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.TraceInformation(message);
  }

  TrackPageView(path: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.TrackPageView(path);
  }

  TrackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.TrackUsage(eventName, additional);
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
