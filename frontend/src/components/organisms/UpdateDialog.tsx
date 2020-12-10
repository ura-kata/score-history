import {
  Dialog,
  DialogTitle,
  DialogContent,
  Grid,
  Typography,
  GridList,
  GridListTile,
  GridListTileBar,
  DialogActions,
  Button,
  makeStyles,
  createStyles,
  Theme,
  colors
} from '@material-ui/core';
import React from 'react';
import { useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import PracticeManagerApiClient from '../../PracticeManagerApiClient';

const client = new PracticeManagerApiClient("http://localhost:5000/");

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    img:{
      height: "50vh"
    },
    imageDropZoneRoot: {
      margin:"20px",
    },
    imageDropZone:{
      width: "100%",
      height: "50px",
      cursor: "pointer",
      backgroundColor: colors.grey[200],
      textAlign: "center",
      '&:hover': {
        backgroundColor: colors.yellow[100],
      }
    },
    imageList:{
      backgroundColor: colors.grey[100],
    },
    table: {
      height: "auto",
      width: '100%'
    }
  })
);

interface FileData{
  fileUrl: string;
  file: File;
}

export interface UpdateDialogProps{
  open: boolean;
  scoreName: string;
  onUploaded?: ((event: {}) => void) | undefined;
  onCanceled?: ((event: {}) => void) | undefined;
}

const UpdateDialog = (props: UpdateDialogProps)=>{

  const classes = useStyles();

  const _open = props.open;
  const _scoreName = props.scoreName;
  const _onUploaded = props.onUploaded;
  const _onCanceled = props.onCanceled;

  const [updateFileDataList, setUpdateFileDataList] = React.useState([] as FileData[]);

  const onUpdateDrop = useCallback((acceptedFiles) => {
    setUpdateFileDataList([])
    const loadedFileDataList: FileData[] = [];

    acceptedFiles.forEach((f: File) => {
      const reader = new FileReader();

      reader.onabort = () => console.log('ファイルの読み込み中断');
      reader.onerror = () => console.log('ファイルの読み込みエラー');
      reader.onload = (e) => {
        // 正常に読み込みが完了した

          loadedFileDataList.push({
            fileUrl: e.target?.result as string,
            file: f
          });
          setUpdateFileDataList([...loadedFileDataList]);

      };

      reader.readAsDataURL(f);
    })

  },[]);
  const updateDrop = useDropzone({onDrop:onUpdateDrop});

  const handlerUpdate = useCallback(async ()=>{
    try{
      await client.createVersion(_scoreName, updateFileDataList.map(x=>x.file));
      alert('スコアを更新しました');

      setUpdateFileDataList([]);
    } catch(err) {
      alert('スコアの更新に失敗しました');
      return;
    }

    if(_onUploaded){
      _onUploaded({});
    }
  },[_onUploaded, _scoreName, updateFileDataList]);

  const handleCancelClick = useCallback(async ()=>{
    if(_onCanceled){
      _onCanceled({});
    }
  },[_onCanceled]);


  return(
    <Dialog
      open={_open}
      scroll={'paper'}
    >
      <DialogTitle>新しいバージョン</DialogTitle>
      <DialogContent dividers={true}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Typography>{_scoreName}</Typography>
          </Grid>
          <Grid item xs={12}>
            <div className={classes.imageDropZoneRoot}>
              <div {...updateDrop.getRootProps()}>
                <input {...updateDrop.getInputProps()} />
                <div className={classes.imageDropZone}>
                  このエリアをクリックするするか画像ドロップしてアップロードしてください
                </div>
              </div>
            </div>
          </Grid>
          <Grid item xs={12}>
            <GridList className={classes.imageList}>
              {
                updateFileDataList.map((fd, i) => (
                  <GridListTile key={'image' + i.toString()}>
                    <img src={fd.fileUrl} className={classes.img} alt={fd.file.name}></img>
                    <GridListTileBar title={fd.file.name}/>
                  </GridListTile>
                ))
              }
            </GridList>
          </Grid>
        </Grid>

      </DialogContent>
      <DialogActions>
        <Button variant="outlined" color="primary" onClick={handlerUpdate}>更新</Button>
        <Button variant="outlined" color="primary" onClick={handleCancelClick}>キャンセル</Button>
      </DialogActions>
    </Dialog>
  );
}

export default UpdateDialog;
