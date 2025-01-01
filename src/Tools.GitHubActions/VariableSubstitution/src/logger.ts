import * as core from "@actions/core";

export interface ILogger {
    info(message: string): void;

    warning(message: string): void;

    error(message: string): void;
}

export class Logger implements ILogger {

    info(message: string): void {
        core.info(message);
    }

    warning(message: string): void {
        core.warning(message);
    }

    error(message: string): void {
        core.setFailed(message);
    }

}