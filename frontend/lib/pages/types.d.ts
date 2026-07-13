type WorkspaceData = Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>;
type RefreshWorkspaceFunction = () => ReturnType<typeof import('$utils').useWebfeedData>['refresh'];
type UpdateDetails = import('vue').UnwrapRef<
  ReturnType<typeof import('$utils').useUpdateDetails>['updateDetails']
>;

export interface PageProps {
  data: WorkspaceData;
  workspace: WorkspaceData;
  refreshWorkspace: RefreshWorkspaceFunction;
  update: UpdateDetails;
}
