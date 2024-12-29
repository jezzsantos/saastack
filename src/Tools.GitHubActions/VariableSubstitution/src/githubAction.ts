import * as core from "@actions/core";
import * as github from "@actions/github";

export interface IGitHubAction {

    getRequiredWorkflowInput(paramName: string): string;

    getRepositoryOwner(): string;

    getRepositoryName(): string;

    getGitHubSecrets(): any;

    getGitHubVariables(): any;
}

export class GithubAction implements IGitHubAction {
    getGitHubSecrets() {
        const secretsParam = this.getRequiredWorkflowInput('secrets');
        return (secretsParam !== null && secretsParam !== undefined ? JSON.parse(secretsParam) : {}) ?? {};
    }

    getGitHubVariables() {
        const varsParam = this.getRequiredWorkflowInput('variables');
        return (varsParam !== null && varsParam !== undefined ? JSON.parse(varsParam) : {}) ?? {};
    }

    getRepositoryOwner(): string {
        return github.context.repo.owner;
    }

    getRepositoryName(): string {
        return github.context.repo.repo;
    }

    getRequiredWorkflowInput(paramName: string): string {
        return core.getInput(paramName, {required: true});
    }
}