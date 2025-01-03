import fs from "node:fs";

export interface IAppSettingsJsonFileReader {
    readAppSettingsFile(path: string): Promise<any>;
}

export class AppSettingsJsonFileReader implements IAppSettingsJsonFileReader {
    async readAppSettingsFile(path: string): Promise<any> {
        let data: any;
        try {
            const result = await fs.promises.readFile(path);
            data = Buffer.from(result);
        } catch (error) {
            throw new Error(AppSettingsJsonFileReaderErrors.fileCannotBeRead(path));
        }

        const raw = data.toString();
        try {
            return JSON.parse(raw);
        } catch (error) {
            throw new Error(AppSettingsJsonFileReaderErrors.fileDoesNotContainValidJson(path, error));
        }
    }
}

export class AppSettingsJsonFileReaderErrors {
    public static fileCannotBeRead = (path: string): string => `File '${path}' cannot be read from disk, possibly it does not exist, or is not accessible?`;

    public static fileDoesNotContainValidJson = (path: string, error: any): string => `File '${path}' does not contain valid JSON: ${error}`;
}