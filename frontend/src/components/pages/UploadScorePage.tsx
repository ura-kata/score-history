import React, {useCallback, useMemo} from 'react';
import GenericTemplate from '../templates/GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, Button, InputLabel, makeStyles, Theme, colors, GridList, GridListTile, GridListTileBar, Grid, TextField, Dialog, DialogTitle, DialogContent, DialogActions, Typography, Select, MenuItem } from '@material-ui/core'
import { DialogProps } from '@material-ui/core/Dialog'
import { useDropzone } from 'react-dropzone'
import { readBuilderProgram } from 'typescript';
import PracticeManagerApiClient, {Score, SocreVersionMetaUrl} from '../../PracticeManagerApiClient'
import { Alarm } from '@material-ui/icons';
import UploadDialog from '../organisms/UploadDialog';
import VersionDisplayDialog from '../organisms/VersionDisplayDialog'

import ScoreTable, {ScoreTableData} from '../organisms/ScoreTable';

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


const UploadScorePage = () => {
  const classes = useStyles();

  const [uploadDialogOpen, setDialogOpen] = React.useState(false);

  const [socreTableDatas, setSocreTableDatas] = React.useState([] as ScoreTableData[])

  const handleUploadDialogOpen = () => {
    setDialogOpen(true);
  };

  const [versionMetaUrlsSet, setVersionMetaUrlsSet] = React.useState<{[scoreName: string]: SocreVersionMetaUrl[]}>({})

  const updateTable = async ()=>{
    const scores = await client.getScores();

      const r = scores.map((x,i)=>({
        open: x.name,
        update:x.name,
        name: x.name,
        title: x.title,
        lastVersion: x.version_meta_urls.slice(-1)[0].version,
        description: x.description
      } as ScoreTableData));
      setSocreTableDatas(r);

      const urlsSet = {} as {[scoreName: string]: SocreVersionMetaUrl[]};
      scores.forEach(x=>{urlsSet[x.name] = x.version_meta_urls})
      setVersionMetaUrlsSet(urlsSet);
  };

  const handleUpdateTable = useCallback(async (event)=>{
    try{
      await updateTable();
    } catch (err){
      alert('更新に失敗しました');
    }
  },[]);

  // ----------------------------------------------------------------------------------------------------------------

  const [updateFileDataList, setUpdateFileDataList] = React.useState([] as FileData[]);
  const [updateScoreName, setUpdateScoreName] = React.useState('');

  const [updateDialogOpen, setUpdateDialogOpen] = React.useState(false);
  const [updateDialogScroll, setUpdateDialogScroll] = React.useState<DialogProps['scroll']>('paper');

  const handleUpdateDialogClose = () => {
    setUpdateDialogOpen(false);
  };


  const handlerUpdate = useCallback(async ()=>{
    try{
      await client.createVersion(updateScoreName, updateFileDataList.map(x=>x.file));
      alert('スコアを更新しました');
      handleUpdateDialogClose();

      setUpdateFileDataList([]);
    } catch(err) {
      alert('スコアの更新に失敗しました');
      return;
    }

    try{
      await updateTable();
    } catch (err){
      alert('更新に失敗しました');
    }
  },[updateFileDataList, updateScoreName]);

  const handlerUploaded = useCallback(async ()=>{
    try{
      await updateTable();
    } catch (err){
      alert('更新に失敗しました');
    }
  }, []);

  const handlerUploadCanceled = useCallback(async ()=>{
    setDialogOpen(false);
  }, []);

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


  const updateDialog = (
    <Dialog
      open={updateDialogOpen}
      onClose={handleUpdateDialogClose}
      scroll={updateDialogScroll}
    >
      <DialogTitle>新しいバージョン</DialogTitle>
      <DialogContent dividers={true}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Typography>{updateScoreName}</Typography>
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
        <Button variant="outlined" color="primary" onClick={handleUpdateDialogClose}>キャンセル</Button>
      </DialogActions>
    </Dialog>
  );

  // ----------------------------------------------------------------------------------------------------------------


  const [versionDialogOpen, setVersionDialogOpen] = React.useState(false);

  const handleVersionDialogClose = useCallback(async ()=>{
    setVersionDialogOpen(false);
  },[]);

  const [displayScoreName, setDisplayScoreName] = React.useState('');
  const [displayScoreVersionList, setDisplayScoreVersionList] = React.useState([] as number[]);



  const handleTableSelectedChangeRow = useCallback((scoreName: string)=>{
    setUpdateScoreName(scoreName);

    setDisplayScoreName(scoreName);
  },[]);

  const handleOpen = useCallback(()=>{

    if(displayScoreName === ""){
      alert('スコアを選択してください')
      return;
    }
    const urls = versionMetaUrlsSet[displayScoreName];
    if(!urls) return;
    const versionList = urls.map(x=>x.version);
    setDisplayScoreVersionList(versionList);

    setVersionDialogOpen(true);
  },[displayScoreName, versionMetaUrlsSet]);

  const handleUpdate = useCallback(()=>{
    if(updateScoreName === ""){
      alert('スコアを選択してください')
      return;
    }

    setUpdateDialogOpen(true);
  },[updateScoreName]);

  const scoreTableElement = useMemo(()=>(
    <ScoreTable
      title={""}
      data={socreTableDatas}
      onSelectedChangeRow={handleTableSelectedChangeRow}
    />
  ),[socreTableDatas, handleTableSelectedChangeRow]);

  return (
    <GenericTemplate title="スコアの一覧">
      <div>
        <Grid container spacing={3}>
          <Grid item xs={3}>
            <Button variant="outlined" color="primary" onClick={handleUpdateTable}>スコアの取得</Button>
          </Grid>
          <Grid item xs={3}>
            <Button variant="outlined" color="primary" onClick={handleUploadDialogOpen}>アップロードする</Button>
          </Grid>
          <Grid item xs={6} />
          {/*--------------------------------------------------*/}
          <Grid item xs={2}>
            <Button variant="outlined" color="primary" onClick={handleOpen}>表示</Button>
          </Grid>
          <Grid item xs={2}>
            <Button variant="outlined" color="primary" onClick={handleUpdate}>更新</Button>
          </Grid>
          <Grid item xs={8} />
          {/*--------------------------------------------------*/}
          <Grid item xs={12}>
            {/* <div className={classes.table}>
              <DataGrid rows={rows} columns={columns} pageSize={10} />
            </div> */}
            <div className={classes.table}>
              {scoreTableElement}
            </div>
          </Grid>
        </Grid>

        <UploadDialog onUploaded={handlerUploaded} onCanceled={handlerUploadCanceled} open={uploadDialogOpen}/>

        <VersionDisplayDialog
          open={versionDialogOpen}
          scoreName={displayScoreName}
          versions={displayScoreVersionList}
          onCloseClicked={handleVersionDialogClose}
        />

        {updateDialog}

      </div>
    </GenericTemplate>
  )
}

export default UploadScorePage;
