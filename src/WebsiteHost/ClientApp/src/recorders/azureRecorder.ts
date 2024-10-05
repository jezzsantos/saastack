import { ApplicationInsights, SeverityLevel as AISeverityLevel } from "@microsoft/applicationinsights-web";
import { SeverityLevel } from "../recorder";
import { BrowserRecorder } from "./browserRecorder";

const appInsightsKey = process.env.APPLICATION_INSIGHTS_INSTRUMENTATION_KEY;
const applicationInsightsEnabled = process.env.NODE_ENV === "production";

if (applicationInsightsEnabled && !appInsightsKey) {
  console.warn("SaaStack: Application Insights instrumentation key is missing from the environment variable file.");
}

const appInsights = new ApplicationInsights({
  config: {
    instrumentationKey: process.env.APPLICATION_INSIGHTS_INSTRUMENTATION_KEY
  }
});

if (applicationInsightsEnabled) {
  appInsights.loadAppInsights();
}

export class AzureRecorder extends BrowserRecorder {
  Crash(error: Error, message?: string): void {
    super.Crash(error, message);
    if (!window.isTestingOnly) {
      appInsights.trackException({
        exception: error,
        severityLevel: AISeverityLevel.Error,
        properties: message ? { message } : undefined
      });
    }
  }

  Trace(message: string, severityLevel: SeverityLevel): void {
    super.Trace(message, severityLevel);
    if (!window.isTestingOnly) {
      appInsights.trackTrace({
        message,
        severityLevel: toAISeverity(severityLevel)
      });
    }
  }

  TraceDebug(message: string): void {
    super.TraceDebug(message);
    if (!window.isTestingOnly) {
      appInsights.trackTrace({
        message,
        severityLevel: AISeverityLevel.Verbose
      });
    }
  }

  TraceInformation(message: string): void {
    super.TraceInformation(message);
    if (!window.isTestingOnly) {
      appInsights.trackTrace({
        message,
        severityLevel: AISeverityLevel.Information
      });
    }
  }

  TrackPageView(path: string): void {
    super.TrackPageView(path);
    if (!window.isTestingOnly) {
      appInsights.trackPageView({
        name: path
      });
    }
  }

  TrackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    super.TrackUsage(eventName, additional);
    if (!window.isTestingOnly) {
      appInsights.trackMetric({ name: eventName, average: 1 });
    }
  }
}

function toAISeverity(severityLevel: SeverityLevel): AISeverityLevel {
  switch (severityLevel) {
    case SeverityLevel.Debug:
      return AISeverityLevel.Verbose;
    case SeverityLevel.Information:
      return AISeverityLevel.Information;
    case SeverityLevel.Warning:
      return AISeverityLevel.Warning;
    case SeverityLevel.Error:
      return AISeverityLevel.Error;

    default:
      return AISeverityLevel.Critical;
  }
}
