import {ConfigurationSet, ConfigurationSetMessages} from "./configurationSet";
import {ILogger} from "./logger";
import {ISettingsFile} from "./settingsFile";
import {AppSettingRequiredVariable} from "./settingsFileProcessor";

describe('verify', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should return true, when the set contains no required', async () => {

        const set = new ConfigurationSet("apath", []);
        set.accumulateVariables();

        const result = set.verify(logger, {}, {});

        expect(result).toBe(true);
        expect(logger.info).toHaveBeenCalledWith(ConfigurationSetMessages.verificationSucceeded("apath"));
    });

    it('should return true, but warn, when the set defines required variable, but no variable exists to substitute', async () => {

        const settingsFile: jest.Mocked<ISettingsFile> = {
            path: "afile.json",
            hasRequired: true,
            variables: ["aname"],
            requiredVariables: [new AppSettingRequiredVariable("arequired", "AREQUIRED")],
            substitute: jest.fn(),
        };
        const set = new ConfigurationSet("apath", [settingsFile]);
        set.accumulateVariables();

        const result = set.verify(logger, {}, {});

        expect(result).toBe(true);
        expect(logger.warning).toHaveBeenCalledWith(ConfigurationSetMessages.redundantVariables(["arequired"]));
        expect(logger.info).toHaveBeenCalledWith(ConfigurationSetMessages.verificationSucceeded("apath"));
    });

    it('should return false, when the set defines required variable, but no variable/secret exists in GitHub', async () => {

        const settingsFile: jest.Mocked<ISettingsFile> = {
            path: "afile.json",
            hasRequired: true,
            variables: ["arequired-arequired:aname"],
            requiredVariables: [new AppSettingRequiredVariable("arequired-arequired:aname", "AREQUIRED_AREQUIRED_ANAME")],
            substitute: jest.fn(),
        };
        const set = new ConfigurationSet("apath", [settingsFile]);
        set.accumulateVariables();

        const result = set.verify(logger, {}, {});

        expect(result).toBe(false);
        expect(logger.info).not.toHaveBeenCalledWith(ConfigurationSetMessages.verificationSucceeded("apath"));
        expect(logger.error).toHaveBeenCalledWith(ConfigurationSetMessages.missingRequiredVariables([{
            requiredVariable: "arequired-arequired:aname",
            gitHubVariableName: "AREQUIRED_AREQUIRED_ANAME"
        }]));
        expect(logger.error).toHaveBeenCalledWith(ConfigurationSetMessages.verificationFailed("apath"));
    });

    it('should return true, when the set defines required variable, and variable exists in GitHub', async () => {

        const settingsFile: jest.Mocked<ISettingsFile> = {
            path: "afile.json",
            hasRequired: true,
            variables: ["arequired-arequired:aname"],
            requiredVariables: [new AppSettingRequiredVariable("arequired-arequired:aname", "AREQUIRED_AREQUIRED_ANAME")],
            substitute: jest.fn(),
        };
        const set = new ConfigurationSet("apath", [settingsFile]);
        set.accumulateVariables();

        const result = set.verify(logger, {"AREQUIRED_AREQUIRED_ANAME": "avalue"}, {});

        expect(result).toBe(true);
        expect(logger.info).toHaveBeenCalledWith(ConfigurationSetMessages.foundConfirmedVariables([{
            requiredVariable: "arequired-arequired:aname",
            gitHubVariableName: "AREQUIRED_AREQUIRED_ANAME"
        }]));
        expect(logger.info).toHaveBeenCalledWith(ConfigurationSetMessages.verificationSucceeded("apath"));
    });

    it('should return true, when the set defines required variable, and secret exists in GitHub', async () => {

        const settingsFile: jest.Mocked<ISettingsFile> = {
            path: "afile.json",
            hasRequired: true,
            variables: ["arequired-arequired:aname"],
            requiredVariables: [new AppSettingRequiredVariable("arequired-arequired:aname", "AREQUIRED_AREQUIRED_ANAME")],
            substitute: jest.fn(),
        };
        const set = new ConfigurationSet("apath", [settingsFile]);
        set.accumulateVariables();

        const result = set.verify(logger, {}, {"AREQUIRED_AREQUIRED_ANAME": "avalue"});

        expect(result).toBe(true);
        expect(logger.info).toHaveBeenCalledWith(ConfigurationSetMessages.foundConfirmedVariables([{
            requiredVariable: "arequired-arequired:aname",
            gitHubVariableName: "AREQUIRED_AREQUIRED_ANAME"
        }]));
        expect(logger.info).toHaveBeenCalledWith(ConfigurationSetMessages.verificationSucceeded("apath"));
    });
});