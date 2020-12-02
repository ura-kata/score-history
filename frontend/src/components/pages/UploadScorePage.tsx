import React, {useCallback} from 'react';
import GenericTemplate from '../templates/GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, Button, InputLabel, makeStyles, Theme, colors, GridList, GridListTile, GridListTileBar, Grid, TextField, Dialog, DialogTitle, DialogContent, DialogActions } from '@material-ui/core'
import { DialogProps } from '@material-ui/core/Dialog'
import { DataGrid, ColDef, ValueGetterParams, Row } from '@material-ui/data-grid';
import { useDropzone } from 'react-dropzone'
import { readBuilderProgram } from 'typescript';
import PracticeManagerApiClient, {Score} from '../../PracticeManagerApiClient'
import { Alarm } from '@material-ui/icons';

const client = new PracticeManagerApiClient("http://localhost:5000/");

const columns: ColDef[] = [
  {field: 'name', headerName: '名前', width: 200},
  {field: 'title', headerName: 'タイトル', width: 200},
  {field: 'description', headerName: '説明', width: 500},
]

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
      height: 400,
      width: '100%'
    }
  })
);

interface FileData{
  fileUrl: string;
  file: File;
}

interface Row{
  id: number;
  name: string;
  title: string;
  description: string;
}

const UploadScorePage = () => {
  const classes = useStyles();
  const [fileDataList, setFileDataList] = React.useState([] as FileData[]);
  const [uploadScoreName, setUploadScoreName] = React.useState("");
  const [uploadScoreTitle, setUploadScoreTitle] = React.useState("");
  const [uploadScoreDescription, setUploadScoreDescription] = React.useState("");

  const [dialogOpen, setDialogOpen] = React.useState(false);
  const [dialogScroll, setDialogScroll] = React.useState<DialogProps['scroll']>('paper');

  const [rows, setRows] = React.useState([] as Row[])

  const handleDialogOpen = () => {
    setDialogOpen(true);
    setDialogScroll('paper');
  };

  const handleDialogClose = () => {
    setDialogOpen(false);
  }

  const onDrop = useCallback((acceptedFiles) => {
    setFileDataList([])
    const loadedFileDataList: FileData[] = [];
    console.log("onDrop");

    acceptedFiles.forEach((f: File) => {
      console.log(f);
      const reader = new FileReader();

      reader.onabort = () => console.log('ファイルの読み込み中断');
      reader.onerror = () => console.log('ファイルの読み込みエラー');
      reader.onload = (e) => {
        // 正常に読み込みが完了した

          loadedFileDataList.push({
            fileUrl: e.target?.result as string,
            file: f
          });
          setFileDataList([...loadedFileDataList]);

      };

      reader.readAsDataURL(f);
    })

  },[]);
  const {getRootProps, getInputProps, isDragActive} = useDropzone({onDrop});

  const handlerUpload = useCallback(async ()=>{
    try{
      await client.createScore({
        name: uploadScoreName,
        title: uploadScoreTitle,
        description: uploadScoreDescription
      });
      await client.createVersion(uploadScoreName, fileDataList.map(x=>x.file));
      alert('画像をアップロードしました');
      handleDialogClose();

      setUploadScoreName("");
      setUploadScoreTitle("");
      setUploadScoreDescription("");
      setFileDataList([]);
    } catch(err) {
      alert('ファイルのアップロードに失敗しました');
    }
  },[fileDataList, uploadScoreName, uploadScoreTitle, uploadScoreDescription]);

  const handlerUploadScoreName = useCallback(async (event)=>{
    setUploadScoreName(event.target.value);
  }, []);
  const handlerUploadScoreTitle = useCallback(async (event)=>{
    setUploadScoreTitle(event.target.value);
  }, []);
  const handlerUploadScoreDescription = useCallback(async (event)=>{
    setUploadScoreDescription(event.target.value);
  }, []);

  const handleUpdateTable = useCallback(async (event)=>{
    try{
      const scores = await client.getScores();

      const r = scores.map((x,i)=>({
        id: i,
        name: x.name,
        title: x.title,
        description: x.description
      }));

      setRows(r);
    } catch (err){
      alert('更新に失敗しました');
    }
  },[]);

  return (
    <GenericTemplate title="スコアの一覧">
      <div>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Button variant="outlined" color="primary" onClick={handleUpdateTable}>更新</Button>
          </Grid>
          <Grid item xs={12}>
            <div className={classes.table}>
              <DataGrid rows={rows} columns={columns} pageSize={10} />
            </div>
          </Grid>
          <Grid item xs={12}>
            <Button variant="outlined" color="primary" onClick={handleDialogOpen}>アップロードする</Button>
          </Grid>
        </Grid>

        <Dialog
          open={dialogOpen}
          onClose={handleDialogClose}
          scroll={dialogScroll}
        >
          <DialogTitle>スコアをアップロードします</DialogTitle>
          <DialogContent dividers={true}>
            <Grid container spacing={3}>
              <Grid item xs={12}>
                <TextField value={uploadScoreName} onChange={handlerUploadScoreName} label={'スコアの名前'}></TextField>
              </Grid>
              <Grid item xs={12}>
                <TextField value={uploadScoreTitle} onChange={handlerUploadScoreTitle} label={'スコアのタイトル'}></TextField>
              </Grid>
              <Grid item xs={12}>
                <TextField value={uploadScoreDescription} onChange={handlerUploadScoreDescription} label={'スコアの説明'}></TextField>
              </Grid>
              <Grid item xs={12}>
                <div className={classes.imageDropZoneRoot}>
                  <div {...getRootProps()}>
                    <input {...getInputProps()} />
                    <div className={classes.imageDropZone}>
                      このエリアをクリックするするか画像ドロップしてアップロードしてください
                    </div>
                  </div>
                </div>
              </Grid>
              <Grid item xs={12}>
                <GridList className={classes.imageList}>
                  {
                    fileDataList.map((fd, i) => (
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
            <Button variant="outlined" color="primary" onClick={handlerUpload}>アップロード</Button>
            <Button variant="outlined" color="primary" onClick={handleDialogClose}>キャンセル</Button>
          </DialogActions>


        </Dialog>
      </div>
    </GenericTemplate>
  )
}

export default UploadScorePage;
