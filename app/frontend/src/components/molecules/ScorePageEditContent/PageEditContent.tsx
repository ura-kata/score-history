import {
  makeStyles,
  Theme,
  createStyles,
  Button,
  Drawer,
  IconButton,
  colors,
  Paper,
  styled,
} from "@material-ui/core";
import AddIcon from "@material-ui/icons/Add";
import { useEffect, useMemo, useRef, useState } from "react";
import { privateScoreItemUrlGen, scoreClientV2 } from "../../../global";
import {
  NewlyScoreItem,
  NewScorePage,
  PatchScorePage,
  ScorePage,
} from "../../../ScoreClientV2";
import AddCircleOutlineIcon from "@material-ui/icons/AddCircleOutline";
import CloseIcon from "@material-ui/icons/Close";
import { useDropzone } from "react-dropzone";
import { Skeleton } from "@material-ui/lab";
import { FileRejection, DropEvent } from "react-dropzone";
import BackupIcon from "@material-ui/icons/Backup";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    toolBar: {
      width: "100%",
      height: "50px",
      display: "flex",
      "& button": {
        margin: "4px",
      },
    },
    editPageContainer: {
      width: "100%",
      display: "flex",
      flexWrap: "wrap",
    },
    itemRoot: {
      height: "280px",
      width: "220px",
      display: "flex",
      margin: "5px",
    },
    itemAddRoot: {
      height: "280px",
      width: "200px",
      display: "flex",
      margin: "5px",
    },
    itemContainer: {
      width: "200px",
      height: "100%",
    },
    newItemContainer: {
      backgroundColor: colors.lightGreen[100],
    },
    itemDivider: { height: "100%", width: "20px" },
    itemToolBar: {
      width: "100%",
      display: "flex",
      justifyContent: "flex-end",
      justifyItems: "center",
      textAlign: "center",
      height: "50px",
    },
    itemButton: {
      height: "100%",
      width: "100%",
      padding: 0,
      "& span": {
        height: "100%",
        width: "100%",
      },
    },
    itemButtonContainer: {
      height: "100%",
      display: "flex",
      alignItems: "inherit",
      justifyContent: "flex-start",
      textAlign: "center",
      flexFlow: "column",
      "& div": {
        height: "200px",
        width: "100%",
        display: "flex",
        justifyContent: "center",
        "& img": {
          height: "100%",
        },
        "& span": {
          height: "100%",
          width: "150px",
        },
      },
      "& p": { margin: "0", height: "30px" },
    },
    itemDividerButton: {
      height: "100%",
      width: "100%",
      minWidth: 0,
      padding: "6px",
      "& span": {
        height: "100%",
        width: "100%",
      },
    },
    itemImg: {
      height: "100%",
    },
    drawerContainer: { width: "50vw", display: "flex", flexFlow: "column" },
    drawerToolBar: {
      width: "100%",
      height: "50px",
      display: "flex",
      justifyContent: "flex-end",
    },
    drawerContent: { width: "100%" },
    imageDropZone: { width: "100%" },
    imageListContainer: { width: "100%", display: "flex", flexWrap: "wrap" },
    uploadImg: { height: "200px" },
    sortLayerRoot: {
      width: "100%",
      height: "100%",
    },
  })
);

//------------------------------------------------------------------------------

interface OnDropFile {
  file: File;
  ope: "insert" | "replace" | "add";
  setLoadedCallback: (callback: (url: string) => void) => void;
}

function CreateOnDropHandle(
  ope: "insert" | "replace" | "add",
  onDropFiles: (files: OnDropFile[]) => void
) {
  return (
    acceptedFiles: File[],
    fileRejections: FileRejection[],
    event: DropEvent
  ) => {
    const elm = event.target as HTMLElement;

    const sortedFiles = [...acceptedFiles].sort((x, y) => {
      if (x.name > y.name) {
        return 1;
      }
      if (x.name < y.name) {
        return -1;
      }
      return 0;
    });

    const callbacks = sortedFiles.map((_) => (url: string) => {});
    const dropFiles = sortedFiles.map((f, index) => {
      const result: OnDropFile = {
        file: f,
        setLoadedCallback: (callback: (url: string) => void) => {
          callbacks[index] = callback;
        },
        ope: ope === "add" ? "add" : index === 0 ? ope : "insert",
      };
      return result;
    });

    onDropFiles(dropFiles);

    sortedFiles.forEach((f, index) => {
      const reader = new FileReader();

      reader.onabort = () => console.log("ファイルの読み込み中断");
      reader.onerror = () => console.log("ファイルの読み込みエラー");
      reader.onload = (e) => {
        // 正常に読み込みが完了した
        console.log("ファイルの読み込み完了");

        const callback = callbacks[index];
        if (callback) callback(e.target?.result as string);
      };

      reader.readAsDataURL(f);
    });
  };
}
//------------------------------------------------------------------------------

interface OpeItemProps {
  item: AfterOpeItem;
  onDropFiles: (files: OnDropFile[]) => void;
  onRemoveClick: () => void;
  onClick: () => void;
  onMouseDown: () => void;
  onMouseUp: () => void;
}
function OpeItem(props: OpeItemProps) {
  const _item = props.item;
  const _onDropFiles = props.onDropFiles;
  const _onRemoveClick = props.onRemoveClick;
  const _onClick = props.onClick;
  const _onMouseDown = props.onMouseDown;
  const _onMouseUp = props.onMouseUp;
  const classes = useStyles();

  const insertDropzone = useDropzone({
    onDrop: CreateOnDropHandle("insert", _onDropFiles),
  });
  const replaceDropzone = useDropzone({
    onDrop: CreateOnDropHandle("replace", _onDropFiles),
  });
  const replaceDropDropzone = useDropzone({
    onDrop: CreateOnDropHandle("replace", _onDropFiles),
    noClick: true,
    noKeyboard: true,
  });

  return (
    <div className={classes.itemRoot}>
      <div className={classes.itemDivider}>
        <div
          {...insertDropzone.getRootProps()}
          style={{ width: "100%", height: "100%" }}
        >
          <Button className={classes.itemDividerButton}>
            <AddIcon />
          </Button>
        </div>
      </div>
      <div
        className={
          classes.itemContainer +
          (_item.isNew ? " " + classes.newItemContainer : "")
        }
      >
        <div className={classes.itemToolBar}>
          <IconButton
            onClick={(event) => {
              if (_onRemoveClick) _onRemoveClick();
            }}
          >
            <CloseIcon />
          </IconButton>

          <div {...replaceDropzone.getRootProps()}>
            <IconButton>
              <BackupIcon />
            </IconButton>
          </div>
        </div>
        <div
          style={{
            width: "100%",
            height: "100%",
          }}
        >
          <div
            className={classes.itemButtonContainer}
            {...replaceDropDropzone.getRootProps()}
          >
            <div
              onClick={() => {
                _onClick();
              }}
              onMouseDown={() => _onMouseDown()}
              onMouseUp={() => _onMouseUp()}
            >
              {_item.thumbnailSrc ? (
                <img
                  src={_item.thumbnailSrc}
                  className={classes.itemImg}
                  onDragStart={(e) => e.preventDefault()}
                />
              ) : (
                <Skeleton variant="rect" />
              )}
            </div>
            <p>{_item.isNew ? "new" : _item.page?.page || "none"}</p>
          </div>
        </div>
      </div>
      <input style={{ display: "none" }} {...insertDropzone.getInputProps()} />
      <input style={{ display: "none" }} {...replaceDropzone.getInputProps()} />
      <input
        style={{ display: "none" }}
        {...replaceDropDropzone.getInputProps()}
      />
    </div>
  );
}

//-----------------------------------------------------------------------

interface AddOpeItemProps {
  onDropFiles: (files: OnDropFile[]) => void;
}
function AddOpeItem(props: AddOpeItemProps) {
  const classes = useStyles();

  const _onDropFiles = props.onDropFiles;

  const addDropzone = useDropzone({
    onDrop: CreateOnDropHandle("add", _onDropFiles),
    noClick: false,
    noKeyboard: true,
  });

  return (
    <div className={classes.itemAddRoot}>
      <div
        {...addDropzone.getRootProps()}
        style={{ width: "100%", height: "100%" }}
      >
        <Button className={classes.itemButton}>
          <AddCircleOutlineIcon />
        </Button>
      </div>
      <input style={{ display: "none" }} {...addDropzone.getInputProps()} />
    </div>
  );
}

//-----------------------------------------------------------------------

const CustomPaper = styled(Paper)({
  backgroundColor: "#00000000",
});

//-----------------------------------------------------------------------

type OpeKinds = "add" | "insert" | "remove" | "replace" | "sort";
interface Ope {
  kind?: OpeKinds;
  index?: number;
  dropFileIndex?: number;
  oldIndex?: number;
}

interface AfterOpeItem {
  id: string;
  thumbnailSrc?: string;
  orginSrc?: string;
  page?: ScorePage;
  isNew?: boolean;
  dropFileIndex?: number;
}

export interface PageEditContentProps {
  ownerId?: string;
  scoreId?: string;
  pages?: ScorePage[];
  onCompleted?: () => void;
  onCancel?: () => void;
}

export default function PageEditContent(props: PageEditContentProps) {
  const classes = useStyles();
  const _ownerId = props.ownerId;
  const _scoreId = props.scoreId;
  const _pages = props.pages;

  const _onCompleted = props.onCompleted;
  const _onCancel = props.onCancel;

  const [openDrawer, setOpenDrawer] = useState(false);
  /** オペレーションのリスト */
  const [opeList, setOpeList] = useState<Ope[]>([]);
  /** 最後にロードが完了したファイルのインデックス */
  const [latestLoadFileIndex, setLatestLoadFileIndex] = useState<number>();
  /** ドロップしたファイルのリスト */
  const [dropFileList, setDropFileList] = useState<File[]>([]);

  /** 読み込んだ画像の blob の URL */
  const loadedFileUrlSet = useRef<{ [index: number]: string }>({});
  /** アップロードが完了したデータ */
  const successUploadedDropFileIndexSet = useRef<{
    [index: number]: NewlyScoreItem;
  }>({});

  const [oldSortTargetIndex, setOldSortTargetIndex] =
    useState<number | undefined>();

  /** オペレーションを適用した後のアイテムリスト */
  const { afterOpeItemList, removePageIds } = useMemo<{
    afterOpeItemList: AfterOpeItem[];
    removePageIds: number[];
  }>(() => {
    if (!_ownerId) return { afterOpeItemList: [], removePageIds: [] };
    if (!_scoreId) return { afterOpeItemList: [], removePageIds: [] };
    if (!_pages) return { afterOpeItemList: [], removePageIds: [] };

    const removePageIds: number[] = [];
    const result = _pages.map((p, index): AfterOpeItem => {
      const thumbnailSrc = privateScoreItemUrlGen.getThumbnailImageUrl(
        _ownerId,
        _scoreId,
        p
      );
      const originSrc = privateScoreItemUrlGen.getImageUrl(
        _ownerId,
        _scoreId,
        p
      );
      return {
        id: "org_" + p.id,
        thumbnailSrc: thumbnailSrc,
        orginSrc: originSrc,
        page: p,
      };
    });

    opeList.forEach((ope, index) => {
      const id = "ope_" + index;
      switch (ope.kind) {
        case "add": {
          const dropFileIndex = ope.dropFileIndex;
          const fileUrl =
            dropFileIndex !== undefined
              ? loadedFileUrlSet.current[dropFileIndex]
              : undefined;

          result.push({
            id: id,
            thumbnailSrc: fileUrl,
            orginSrc: fileUrl,
            isNew: true,
            dropFileIndex: dropFileIndex,
          });
          break;
        }
        case "insert": {
          if (
            ope.index !== undefined &&
            0 <= ope.index &&
            ope.index < result.length
          ) {
            const dropFileIndex = ope.dropFileIndex;
            const fileUrl =
              dropFileIndex !== undefined
                ? loadedFileUrlSet.current[dropFileIndex]
                : undefined;

            result.splice(ope.index, 0, {
              id: id,
              thumbnailSrc: fileUrl,
              orginSrc: fileUrl,
              isNew: true,
              dropFileIndex: dropFileIndex,
            });
          }
          break;
        }
        case "remove": {
          if (ope.index === undefined) return;
          if (ope.index < 0 || result.length <= ope.index) return;
          const afi = result[ope.index];
          result.splice(ope.index, 1);
          if (afi.page?.id === undefined) return;
          removePageIds.push(afi.page.id);
          break;
        }
        case "replace": {
          if (
            ope.index !== undefined &&
            0 <= ope.index &&
            ope.index < result.length
          ) {
            const dropFileIndex = ope.dropFileIndex;
            const fileUrl =
              dropFileIndex !== undefined
                ? loadedFileUrlSet.current[dropFileIndex]
                : undefined;

            result.splice(ope.index, 1, {
              ...result[ope.index],
              id: id,
              thumbnailSrc: fileUrl,
              orginSrc: fileUrl,
              isNew: true,
              dropFileIndex: dropFileIndex,
            });
          }
          break;
        }
        case "sort": {
          if (ope.index === undefined || ope.oldIndex === undefined) return;
          if (ope.index < 0 || result.length <= ope.index) return;
          if (ope.oldIndex < 0 || result.length <= ope.oldIndex) return;

          const old = result[ope.oldIndex];
          if (ope.index < ope.oldIndex) {
            result.splice(ope.oldIndex, 1);
            result.splice(ope.index, 0, old);
          } else {
            result.splice(ope.index, 0, old);
            result.splice(ope.oldIndex, 1);
          }
        }
      }
    });

    return { afterOpeItemList: result, removePageIds: removePageIds };
  }, [_ownerId, _scoreId, _pages, opeList, latestLoadFileIndex]);

  const handleOnDrawerClose = () => {
    setOpenDrawer(false);
  };

  const handleOnApplyClick = async () => {
    // アップロード処理を行う

    if (!_scoreId) return;
    const newPages: NewScorePage[] = [];
    const patchPages: PatchScorePage[] = [];
    for (let i = 0; i < afterOpeItemList.length; ++i) {
      const aoi = afterOpeItemList[i];

      /** ページの更新情報 */
      let item: { itemId: string; objectName: string } | undefined = undefined;

      if (aoi.dropFileIndex !== undefined) {
        const uploadedDropFile =
          successUploadedDropFileIndexSet.current[aoi.dropFileIndex];

        if (uploadedDropFile) {
          // すでにアップロードしている
          item = {
            itemId: uploadedDropFile.itemInfo.itemId,
            objectName: uploadedDropFile.itemInfo.objectName,
          };
        } else {
          // 画像をアップロードする
          const file = dropFileList[aoi.dropFileIndex];

          if (file) {
            try {
              const newlyItem = await scoreClientV2.uploadItem(_scoreId, file);

              successUploadedDropFileIndexSet.current[aoi.dropFileIndex] =
                newlyItem;
              item = {
                itemId: newlyItem.itemInfo.itemId,
                objectName: newlyItem.itemInfo.objectName,
              };
            } catch (err) {
              // TODO とりあえずアラート
              alert(err);
              console.log(err);
              return;
            }
          }
        }
      }

      if (item) {
        newPages.push({
          itemId: item.itemId,
          objectName: item.objectName,
          page: i.toString(),
        });
      } else if (aoi.page && aoi.page.page !== i.toString()) {
        const page = aoi.page;
        patchPages.push({
          itemId: page.itemId,
          targetPageId: page.id,
          objectName: page.objectName,
          page: i.toString(),
        });
      }
    }
    // ページの更新処理を行う
    try {
      if (0 < newPages.length) {
        await scoreClientV2.addPages(_scoreId, newPages);
      }
      if (0 < patchPages.length) {
        await scoreClientV2.updatePages(_scoreId, patchPages);
      }
      if (0 < removePageIds.length) {
        await scoreClientV2.removePages(_scoreId, removePageIds);
      }
    } catch (err) {
      alert(err);
      console.log(err);
      return;
    }

    // データを初期化する
    setOpeList([]);
    setDropFileList([]);
    setLatestLoadFileIndex(undefined);
    loadedFileUrlSet.current = {};
    successUploadedDropFileIndexSet.current = {};
    if (_onCompleted) {
      _onCompleted();
    }
  };
  const handleOnCancelClick = () => {
    // データを初期化する
    setOpeList([]);
    setDropFileList([]);
    setLatestLoadFileIndex(undefined);
    loadedFileUrlSet.current = {};
    successUploadedDropFileIndexSet.current = {};

    if (_onCancel) {
      _onCancel();
    }
  };

  const handleOnAddDropFiles = (files: OnDropFile[]) => {
    const newDropFileList = [...dropFileList];
    const newOpeList = [...opeList];

    files.forEach((f, index) => {
      const dropFileIndex = newDropFileList.length;
      newDropFileList.push(f.file);
      newOpeList.push({ dropFileIndex, kind: "add" });

      f.setLoadedCallback((url) => {
        loadedFileUrlSet.current[dropFileIndex] = url;
        setLatestLoadFileIndex(dropFileIndex);
      });
    });

    setDropFileList(newDropFileList);
    setOpeList(newOpeList);
  };

  const [sortLayerOpenStart, setsortLayerOpenStart] = useState(false);
  const [sortLayerOpen, setSortLayerOpen] = useState(false);
  useEffect(() => {
    if (!sortLayerOpenStart) return;
    const timeoutId = setTimeout(() => {
      setsortLayerOpenStart(false);
      setSortLayerOpen(true);
    }, 500);
    return () => clearTimeout(timeoutId);
  }, [sortLayerOpenStart]);

  return (
    <div style={{ width: "100%", position: "relative" }}>
      <div className={classes.toolBar}>
        <Button variant="contained" onClick={handleOnApplyClick}>
          完了
        </Button>
        <Button variant="contained" onClick={handleOnCancelClick}>
          キャンセル
        </Button>
      </div>
      <div className={classes.editPageContainer}>
        {afterOpeItemList.map((x, index) => {
          const handleOnDropFiles = (files: OnDropFile[]) => {
            const newDropFileList = [...dropFileList];
            const newOpeList = [...opeList];

            files.forEach((f, fileIndex) => {
              const dropFileIndex = newDropFileList.length;
              newDropFileList.push(f.file);
              newOpeList.push({
                dropFileIndex,
                kind: f.ope,
                index: index + fileIndex,
              });

              f.setLoadedCallback((url) => {
                loadedFileUrlSet.current[dropFileIndex] = url;
                setLatestLoadFileIndex(dropFileIndex);
              });
            });

            setDropFileList(newDropFileList);
            setOpeList(newOpeList);
          };
          const handleOnRemoveClick = () => {
            const newOpeList = [...opeList];

            newOpeList.push({ kind: "remove", index: index });

            setOpeList(newOpeList);
          };
          const handleOnClick = () => {
            setOpenDrawer(true);
          };
          const handleOnMouseUp = () => {
            setsortLayerOpenStart(false);
          };
          const handleOnMoouseDown = () => {
            setsortLayerOpenStart(true);
            setOldSortTargetIndex(index);
          };
          return (
            <OpeItem
              key={x.id}
              item={x}
              onDropFiles={handleOnDropFiles}
              onRemoveClick={handleOnRemoveClick}
              onClick={handleOnClick}
              onMouseUp={handleOnMouseUp}
              onMouseDown={handleOnMoouseDown}
            />
          );
        })}
        <AddOpeItem onDropFiles={handleOnAddDropFiles} />

        <Drawer anchor="right" open={false} onClose={handleOnDrawerClose}>
          <div className={classes.drawerContainer}>
            <div className={classes.drawerToolBar}>
              <IconButton onClick={handleOnDrawerClose}>
                <CloseIcon />
              </IconButton>
            </div>
            <div className={classes.drawerContent}></div>
            <div className={classes.imageListContainer}>
              {/* {fileDataList.map((fd, index) => (
              <img
                key={index}
                src={fd.fileUrl}
                alt={fd.file.name}
                className={classes.uploadImg}
              ></img>
            ))} */}
            </div>
          </div>
        </Drawer>
      </div>
      <div
        id="test"
        style={{
          position: "absolute",
          display: sortLayerOpen ? undefined : "none",
          top: 0,
          left: 0,
          width: "100%",
          height: "100%",
          backgroundColor: "#00000055",
        }}
        onClick={() => setSortLayerOpen(false)}
      >
        <div className={classes.toolBar} />
        <div
          style={{
            display: "flex",
            width: "100%",
            flexWrap: "wrap",
          }}
        >
          {afterOpeItemList.map((item, index) => {
            const handleOnClick = () => {
              if (oldSortTargetIndex === index) return;

              const newOpeList = [...opeList];

              newOpeList.push({
                index: index,
                oldIndex: oldSortTargetIndex,
                kind: "sort",
              });

              setOpeList(newOpeList);
            };

            return (
              <div key={item.id} className={classes.itemRoot}>
                <div className={classes.itemDivider}></div>
                <div className={classes.itemContainer}>
                  <div
                    style={{
                      width: "100%",
                      height: "100%",
                      border: "dashed",
                      borderWidth: "2px",
                      borderColor: "#000000",
                      visibility:
                        oldSortTargetIndex === index ? "hidden" : undefined,
                    }}
                    onClick={handleOnClick}
                  />
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
