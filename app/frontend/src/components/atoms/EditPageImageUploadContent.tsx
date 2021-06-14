import {
  makeStyles,
  Theme,
  createStyles,
  Button,
  Drawer,
  IconButton,
  colors,
} from "@material-ui/core";
import AddIcon from "@material-ui/icons/Add";
import React, { useCallback, useMemo, useRef, useState } from "react";
import { privateScoreItemUrlGen, scoreClientV2 } from "../../global";
import {
  NewlyScoreItem,
  NewScorePage,
  PatchScorePage,
  ScorePage,
} from "../../ScoreClientV2";
import AddCircleOutlineIcon from "@material-ui/icons/AddCircleOutline";
import CloseIcon from "@material-ui/icons/Close";
import { useDropzone } from "react-dropzone";
import { Skeleton } from "@material-ui/lab";

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
    itemContainer: {
      width: "200px",
      height: "230px",
    },
    newItemContainer: {
      backgroundColor: colors.lightGreen[100],
    },
    itemDivider: { height: "200px", width: "20px" },
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
        "& img": {
          height: "100%",
        },
        "& span": {
          height: "100%",
          width: "150px",
        },
      },
      "& p": { margin: "0" },
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
  })
);

interface Ope {
  kind?: "add" | "insert" | "remove" | "replace";
  index?: number;
  dropFileIndex?: number;
}

interface AfterOpeItem {
  id: string;
  thumbnailSrc?: string;
  orginSrc?: string;
  page?: ScorePage;
  isNew?: boolean;
  dropFileIndex?: number;
}

export interface EditPageImageUploadContentProps {
  ownerId?: string;
  scoreId?: string;
  pages?: ScorePage[];
  onCompleted?: () => void;
  onCancel?: () => void;
}

export default function EditPageImageUploadContent(
  props: EditPageImageUploadContentProps
) {
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

  /** アップロードしたときのオペレーションを保存する Ref */
  const fileDropOpeCurrent =
    useRef<{ kind: "add" | "insert" | "remove" | "replace"; index?: number }>();

  const onDrop = (acceptedFiles: File[]) => {
    const sortedFiles = [...acceptedFiles].sort((x, y) => {
      if (x.name > y.name) {
        return 1;
      }
      if (x.name < y.name) {
        return -1;
      }
      return 0;
    });

    const ope = fileDropOpeCurrent.current;
    const newDropFileList = [...dropFileList];
    const newOpeList = [...opeList];

    const startIndex = newDropFileList.length;

    console.log("onDrop");

    sortedFiles.forEach((f, index) => {
      const dropFileListIndex = startIndex + index;

      newDropFileList.push(f);
      newOpeList.push({
        kind: ope?.kind,
        index: ope?.index !== undefined ? ope?.index + index : undefined,
        dropFileIndex: dropFileListIndex,
      });

      console.log(f);
      const reader = new FileReader();

      reader.onabort = () => console.log("ファイルの読み込み中断");
      reader.onerror = () => console.log("ファイルの読み込みエラー");
      reader.onload = (e) => {
        // 正常に読み込みが完了した
        console.log("ファイルの読み込み完了");

        loadedFileUrlSet.current[dropFileListIndex] = e.target
          ?.result as string;
        setLatestLoadFileIndex(dropFileListIndex);
      };

      reader.readAsDataURL(f);
    });

    setDropFileList(newDropFileList);
    setOpeList(newOpeList);
  };
  const uploadDrop = useDropzone({ onDrop: onDrop });

  /** オペレーションを適用した後のアイテムリスト */
  const afterOpeItemList = useMemo(() => {
    if (!_ownerId) return [];
    if (!_scoreId) return [];
    if (!_pages) return [];
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
          if (
            ope.index !== undefined &&
            0 <= ope.index &&
            ope.index < result.length
          ) {
            result.splice(ope.index, 1);
          }
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
      }
    });

    return result;
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

  return (
    <div style={{ width: "100%" }}>
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
          return (
            <div key={x.id} style={{ display: "flex" }}>
              <div className={classes.itemDivider}>
                <div
                  {...uploadDrop.getRootProps()}
                  style={{ width: "100%", height: "100%" }}
                >
                  <Button
                    className={classes.itemDividerButton}
                    onClick={() => {
                      fileDropOpeCurrent.current = {
                        kind: "insert",
                        index: index,
                      };
                    }}
                  >
                    <AddIcon />
                  </Button>
                </div>
              </div>
              <div
                className={
                  classes.itemContainer +
                  (x.isNew ? " " + classes.newItemContainer : "")
                }
              >
                <div
                  {...uploadDrop.getRootProps()}
                  style={{
                    width: "100%",
                    height: "100%",
                    position: "relative",
                  }}
                >
                  <Button
                    className={classes.itemButton}
                    onClick={() => {
                      fileDropOpeCurrent.current = {
                        kind: "replace",
                        index: index,
                      };
                    }}
                  >
                    <div className={classes.itemButtonContainer}>
                      <div>
                        {x.thumbnailSrc ? (
                          <img
                            src={x.thumbnailSrc}
                            className={classes.itemImg}
                          />
                        ) : (
                          <Skeleton variant="rect" />
                        )}
                      </div>
                      <p>{x.isNew ? "new" : x.page?.page || "none"}</p>
                    </div>
                  </Button>

                  <IconButton
                    style={{ position: "absolute", top: 0, right: 0 }}
                    onClick={(event) => {
                      const newOpeList = [...opeList];
                      newOpeList.push({ kind: "remove", index: index });
                      setOpeList(newOpeList);
                      event.stopPropagation();
                    }}
                  >
                    <CloseIcon />
                  </IconButton>
                </div>
              </div>
            </div>
          );
        })}
        <div className={classes.itemContainer}>
          <div
            {...uploadDrop.getRootProps()}
            style={{ width: "100%", height: "100%" }}
          >
            <Button
              className={classes.itemButton}
              onClick={() => {
                fileDropOpeCurrent.current = {
                  kind: "add",
                };
              }}
            >
              <AddCircleOutlineIcon />
            </Button>
          </div>
        </div>
        <input {...uploadDrop.getInputProps()} />

        <Drawer anchor="right" open={openDrawer} onClose={handleOnDrawerClose}>
          <div className={classes.drawerContainer}>
            <div className={classes.drawerToolBar}>
              <IconButton onClick={handleOnDrawerClose}>
                <CloseIcon />
              </IconButton>
            </div>
            <div className={classes.drawerContent}>
              <div {...uploadDrop.getRootProps()}>
                <input {...uploadDrop.getInputProps()} />
                <div className={classes.imageDropZone}>
                  このエリアをクリックするするか画像ドロップしてアップロードしてください
                </div>
              </div>
            </div>
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
    </div>
  );
}
