import {IAppSettingsJsonFileReader} from "./appSettingsJsonFileReader";

export interface ISettingsFile {
    readonly path: string;
    readonly variables: string[];
    readonly hasRequired: boolean;
    readonly requiredVariables: string[];
}

export class SettingsFile implements ISettingsFile {

    public static DeployProperty: string = "Deploy";
    public static RequiredProperty: string = "Required";
    public static KeysProperty: string = "Keys";
    public static DisabledProperty: string = "Disabled";

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
                    if (SettingsFile.isDeployRequiredKey(element, key, prefix)) {
                        const required = SettingsFile.getDeployRequiredVariables(element);
                        if (required.length > 0) {
                            requiredVariables.push(...required);
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

    private static isDeployRequiredKey(element: any, key: string, prefix: string): boolean {
        if (prefix !== "") {
            return false;
        }

        if (key.toUpperCase() !== SettingsFile.DeployProperty.toUpperCase()) {
            return false;
        }

        if (!element.hasOwnProperty(SettingsFile.RequiredProperty)) {
            return false;
        }


        const required = element[SettingsFile.RequiredProperty];
        if (!required) {
            return false;
        }

        return Array.isArray(required);
    }

    private static getDeployRequiredVariables(element: any): string[] {

        const required = element[SettingsFile.RequiredProperty];
        if (required) {
            if (Array.isArray(required)) {
                let requiredVariables: string[] = [];
                for (let index = 0; index < required.length; index++) {
                    const requiredSection = required[index];

                    if (requiredSection.hasOwnProperty(SettingsFile.KeysProperty)) {

                        if (requiredSection.hasOwnProperty(SettingsFile.DisabledProperty)) {
                            const disabled = requiredSection[SettingsFile.DisabledProperty];
                            if (disabled) {
                                continue;
                            }
                        }

                        const keys = requiredSection[SettingsFile.KeysProperty];
                        if (keys) {
                            requiredVariables.push(...keys);
                        }
                    }
                }
                return requiredVariables;
            }
        }

        return [];
    }
}