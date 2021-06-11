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
import { privateScoreItemUrlGen } from "../../global";
import { ScorePage } from "../../ScoreClientV2";
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
      height: "200px",
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
  isNew?: boolean;
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
  const [opeList, setOpeList] = useState<Ope[]>([]);

  const fileDropOpeCurrent =
    useRef<{ kind: "add" | "insert" | "remove" | "replace"; index?: number }>();

  const [latestLoadFileIndex, setLatestLoadFileIndex] = useState<number>();
  const [dropFileList, setDropFileList] = useState<File[]>([]);
  const loadedFileUrlSet = useRef<{ [index: number]: string }>({});
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
              id: id,
              thumbnailSrc: fileUrl,
              orginSrc: fileUrl,
              isNew: true,
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

  const handleOnApplyClick = () => {
    // TODO アップロード処理を行う
    // TODO ページの更新処理を行う
    if (_onCompleted) {
      _onCompleted();
    }
  };
  const handleOnCancelClick = () => {
    // データを初期化する
    setOpeList([]);
    setDropFileList([]);
    loadedFileUrlSet.current = {};
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
                    {x.thumbnailSrc ? (
                      <img src={x.thumbnailSrc} className={classes.itemImg} />
                    ) : (
                      <Skeleton variant="rect" width={150} height={200} />
                    )}
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
