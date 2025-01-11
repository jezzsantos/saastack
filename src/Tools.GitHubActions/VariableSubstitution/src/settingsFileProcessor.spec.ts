import {GitHubVariables, SettingsFileProcessorMessages} from "./settingsFileProcessor";
import {ILogger} from "./logger";
import {WarningOptions} from "./main";

describe('GitHubVariables', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    describe('getVariableOrSecretValue', () => {
        it('should return undefined if neither variable nor secret', () => {

            const result = GitHubVariables.getVariableOrSecretValue(logger, new WarningOptions(), {}, {}, 'aname');

            expect(result).toBeUndefined();
            expect(logger.warning).not.toHaveBeenCalled();
        });

        it('should return value if variable defined', () => {

            const result = GitHubVariables.getVariableOrSecretValue(logger, new WarningOptions(), {"aname": "avalue"}, {}, 'aname');

            expect(result).toEqual("avalue");
            expect(logger.warning).not.toHaveBeenCalled();
        });

        it('should return value if secret defined', () => {

            const result = GitHubVariables.getVariableOrSecretValue(logger, new WarningOptions(), {}, {"aname": "avalue"}, 'aname');

            expect(result).toEqual("avalue");
            expect(logger.warning).not.toHaveBeenCalled();
        });

        it('should return secret value if both variable and secret defined, and option disabled', () => {

            const result = GitHubVariables.getVariableOrSecretValue(logger, new WarningOptions(undefined, undefined, false), {"aname": "avariablevalue"}, {"aname": "asecretvalue"}, 'aname');

            expect(result).toEqual("asecretvalue");
            expect(logger.warning).not.toHaveBeenCalled();
        });

        it('should return secret value if both variable and secret defined', () => {

            const result = GitHubVariables.getVariableOrSecretValue(logger, new WarningOptions(undefined, undefined, true), {"aname": "avariablevalue"}, {"aname": "asecretvalue"}, 'aname');

            expect(result).toEqual("asecretvalue");
            expect(logger.warning).toHaveBeenCalledWith(SettingsFileProcessorMessages.ambiguousVariableAndSecret('aname'));
        });
    });

    describe('isDefined', () => {
        it('should return false if neither variable nor secret', () => {

            const result = GitHubVariables.isDefined({}, {}, 'aname');

            expect(result).toBe(false);
        });

        it('should return true if variable exists', () => {

            const result = GitHubVariables.isDefined({"aname": "avalue"}, {}, 'aname');

            expect(result).toBe(true);
        });

        it('should return true if secret exists', () => {

            const result = GitHubVariables.isDefined({}, {"aname": "avalue"}, 'aname');

            expect(result).toBe(true);
        });

        it('should return true if both secret and variable exists', () => {

            const result = GitHubVariables.isDefined({"aname": "avalue"}, {"aname": "avalue"}, 'aname');

            expect(result).toBe(true);
        });
    })
});