import {IAppSettingsJsonFileReader} from "./appSettingsJsonFileReader";

export interface ISettingsFile {
    readonly path: string;
    readonly variables: string[];
    readonly hasRequired: boolean;
    readonly requiredVariables: string[];
}

export class SettingsFile implements ISettingsFile {
    private constructor(path: string, variables: string[], requiredVariables: string[]) {
        this._path = path;
        this._variables = variables;
        this._requiredVariables = requiredVariables;
    }

    _path: string;

    get path(): string {
        return this._path;
    }

    _variables: string[];

    get variables(): string[] {
        return this._variables;
    }

    _requiredVariables: string[];

    get requiredVariables(): string[] {
        return this._requiredVariables;
    }

    get hasRequired(): boolean {
        return this._requiredVariables.length > 0;
    }

    public static async create(reader: IAppSettingsJsonFileReader, path: string): Promise<SettingsFile> {

        const json = await reader.readAppSettingsFile(path);
        const variables: string[] = [];
        const requiredVariables: string[] = [];
        SettingsFile.scrapeVariablesRecursively(json, variables, requiredVariables);
        return new SettingsFile(path, variables, requiredVariables);
    }

    private static scrapeVariablesRecursively(json: any, variables: string[], requiredVariables: string[], prefix: string = "") {
        for (const key in json) {
            if (json.hasOwnProperty(key)) {
                const element = json[key];
                const nextPrefix = SettingsFile.createVariablePath(prefix, key);
                if (typeof element === "object") {
                    if (SettingsFile.isTopLevelRequiredKey(element, key, prefix)) {
                        for (let index = 0; index < element.length; index++) {
                            const requiredKey = element[index];
                            requiredVariables.push(requiredKey);
                        }
                    } else {
                        SettingsFile.scrapeVariablesRecursively(element, variables, requiredVariables, nextPrefix);
                    }
                } else {
                    variables.push(nextPrefix);
                }
            }
        }
    }

    private static createVariablePath(prefix: string, key: string): string {
        if (prefix === "") {
            return key;
        }
        return `${prefix}:${key}`;
    }

    private static isTopLevelRequiredKey(element: any, key: string, prefix: string): boolean {
        return (key === "required" || key === "Required")
            && Array.isArray(element)
            && prefix === "";
    }
}