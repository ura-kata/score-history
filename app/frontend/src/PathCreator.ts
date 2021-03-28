export type HomeActionType = "edit" | "page" | "edit-page";

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
  getPagePath(owner: string, scoreName: string, pageIndex: number): string {
    const action: HomeActionType = "page";
    return `/home/${owner}/${scoreName}/${action}/${pageIndex}/`;
  }
}
