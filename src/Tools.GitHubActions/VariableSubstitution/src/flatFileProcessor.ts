import {ILogger} from "./logger";
import {IFileReaderWriter} from "./fileReaderWriter";
import {
    AppSettingRequiredVariable,
    AppSettingVariables,
    GitHubVariables,
    ISettingsFileProcessor,
    SettingsFileProcessorMessages
} from "./settingsFileProcessor";

export class FlatFileProcessor implements ISettingsFileProcessor {
    static PairMatchExpression: RegExp = /(?<name>[\w\d_.]+)(=)(?<value>[\w\d_\-.#{}"'()\[\]=,:;&%$@]*)/;
    static ValueExpression: RegExp = /("*)(#{)(?<variable>[\w\d_\-.()\[\]=,:;&%$@]+)(})("*)/;
    _logger: ILogger;
    _path: string;
    _readerWriter: IFileReaderWriter;

    constructor(logger: ILogger, readerWriter: IFileReaderWriter, path: string) {
        this._logger = logger;
        this._path = path;
        this._readerWriter = readerWriter;
    }


    private static accumulateVariablesRecursively(_logger: ILogger, text: string, variables: string[], requiredVariables: AppSettingRequiredVariable[], _prefix: string = "") {

        if (text.length === 0) {
            return;
        }

        const statements = text.split("\n");
        for (const statement of statements) {
            const trimmed = statement.trim();
            if (trimmed.length === 0) {
                continue;
            }

            const pairMatch = FlatFileProcessor.PairMatchExpression.exec(trimmed);
            if (pairMatch && pairMatch.groups) {
                const name = pairMatch.groups.name;
                const value = pairMatch.groups.value;
                variables.push(name);

                if (value && value.length > 0) {
                    const valueMatch = FlatFileProcessor.ValueExpression.exec(value);
                    if (valueMatch && valueMatch.groups) {
                        const requiredValue = valueMatch.groups.variable;
                        requiredVariables.push(new AppSettingRequiredVariable(name, GitHubVariables.calculateVariableOrSecretName(requiredValue)));
                    }
                }
            }
        }
    }

    private static substituteVariables(logger: ILogger, gitHubVariables: any, gitHubSecrets: any, text: TextValue, _prefix: string = "") {

        if (text.value.length === 0) {
            return;
        }

        const statements = text.value.split("\n");
        for (const statement of statements) {
            const trimmed = statement.trim();
            if (trimmed.length === 0) {
                continue;
            }

            const pairMatch = FlatFileProcessor.PairMatchExpression.exec(trimmed);
            if (pairMatch && pairMatch.groups) {
                const value = pairMatch.groups.value;

                if (value && value.length > 0) {
                    const valueMatch = FlatFileProcessor.ValueExpression.exec(value);
                    if (valueMatch && valueMatch.groups) {
                        const variableName = valueMatch.groups.variable;
                        const githubVariableName = GitHubVariables.calculateVariableOrSecretName(variableName);
                        const gitHubSecretOrVariableValue = GitHubVariables.getVariableOrSecretValue(gitHubVariables, gitHubSecrets, githubVariableName);
                        if (gitHubSecretOrVariableValue) {
                            logger.info(SettingsFileProcessorMessages.substitutingVariable(variableName));
                            text.value = text.value.replace(`\#\{${variableName}\}`, gitHubSecretOrVariableValue);
                        }
                    }
                }
            }
        }
    }

    async substitute(gitHubVariables: any, gitHubSecrets: any): Promise<boolean> {

        if (Object.keys(gitHubVariables).length === 0 && Object.keys(gitHubSecrets).length === 0) {
            return true;
        }

        try {
            const content = await this._readerWriter.readSettingsFile(this._path) as string;
            const textValue = new TextValue(content); //We need to alter the encapsulated value in the next function
            FlatFileProcessor.substituteVariables(this._logger, gitHubVariables, gitHubSecrets, textValue);
            await this._readerWriter.writeSettingsFile(this._path, textValue.value);
            this._logger.info(SettingsFileProcessorMessages.substitutingSucceeded(this._path));
            return true;
        } catch (error) {
            this._logger.error(SettingsFileProcessorMessages.unknownError(this._path, error));
            return false;
        }
    }

    async getVariables(path: string): Promise<AppSettingVariables> {

        const content = await this._readerWriter.readSettingsFile(path) as string;
        const variables: string[] = [];
        const requiredVariables: AppSettingRequiredVariable[] = [];
        FlatFileProcessor.accumulateVariablesRecursively(this._logger, content, variables, requiredVariables);

        return Promise.resolve({variables, requiredVariables});
    }
}

class TextValue {
    value: string;

    constructor(value: string) {
        this.value = value;
    }
}