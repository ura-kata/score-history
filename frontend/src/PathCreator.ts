export type HomeActionType = "edit" | "version" | "edit-page";

export default class PathCreator {
  getHomePath(): string {
    return `/`;
  }
  getDetailPath(owner: string, scoreName: string): string {
    return `/home/${owner}/${scoreName}/`;
  }
  getEditPropertyPath(owner: string, scoreName: string): string {
    const action: HomeActionType = "edit";
    return `/home/${owner}/${scoreName}/${action}/`;
  }
  getEditPagePath(owner: string, scoreName: string): string {
    const action: HomeActionType = "edit-page";
    return `/home/${owner}/${scoreName}/${action}/`;
  }
  getVersionPath(owner: string, scoreName: string, version: string): string {
    const action: HomeActionType = "version";
    return `/home/${owner}/${scoreName}/${action}/${version}/`;
  }
  getPagePath(
    owner: string,
    scoreName: string,
    version: string,
    pageIndex: number
  ): string {
    const action: HomeActionType = "version";
    return `/home/${owner}/${scoreName}/${action}/${version}/${pageIndex}/`;
  }
}
