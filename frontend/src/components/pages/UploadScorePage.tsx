import React, {useCallback, useEffect, useMemo} from 'react';
import GenericTemplate from '../templates/GenericTemplate'
import {
  createStyles,
  Button,
  makeStyles,
  Theme,
  colors,
  Grid,
  Collapse,
  IconButton,
} from '@material-ui/core'
import PracticeManagerApiClient, { SocreVersionMetaUrl} from '../../PracticeManagerApiClient'
import Alert from '@material-ui/lab/Alert';
import UploadDialog from '../organisms/UploadDialog';
import VersionDisplayDialog from '../organisms/VersionDisplayDialog'

import ScoreTable, {ScoreTableData} from '../organisms/ScoreTable';
import UpdateDialog from '../organisms/UpdateDialog';
import { CloseIcon } from '@material-ui/data-grid';

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

const UploadScorePage = () => {
  const classes = useStyles();

  const [selectedScoreName, setSelectedScoreName] = React.useState("");

  const [uploadDialogOpen, setUploadDialogOpen] = React.useState(false);
  const [updateDialogOpen, setUpdateDialogOpen] = React.useState(false);
  const [versionDialogOpen, setVersionDialogOpen] = React.useState(false);

  const [versionMetaUrlsSet, setVersionMetaUrlsSet] = React.useState<{[scoreName: string]: SocreVersionMetaUrl[]}>({})
  const [displayScoreVersionList, setDisplayScoreVersionList] = React.useState([] as number[]);

  const [socreTableDatas, setSocreTableDatas] = React.useState([] as ScoreTableData[])



  const handleUploadDialogOpenClick = () => {
    setUploadDialogOpen(true);
  };


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
      writeError('テーブルの更新に失敗しました');
    }
  },[]);

  const handlerUploaded = useCallback(async ()=>{
    setUploadDialogOpen(false);
    writeSuccess('スコアをアップロードしました');
    try{
      await updateTable();
    } catch (err){
      writeError('テーブルの更新に失敗しました');
    }
  }, []);

  const handlerUploadCanceled = useCallback(async ()=>{
    setUploadDialogOpen(false);
  }, []);


  const handleVersionDialogCloseClicked = useCallback(async ()=>{
    setVersionDialogOpen(false);
  },[]);


  const handleTableSelectedChangeRow = useCallback((scoreName: string)=>{
    setSelectedScoreName(scoreName);
  },[]);

  const handleVersionDialogOpenClick = useCallback(()=>{

    if(selectedScoreName === ""){
      writeWarning('スコアを選択してください');
      return;
    }
    const urls = versionMetaUrlsSet[selectedScoreName];
    if(!urls) return;
    const versionList = urls.map(x=>x.version);
    setDisplayScoreVersionList(versionList);

    setVersionDialogOpen(true);
  },[selectedScoreName, versionMetaUrlsSet]);

  const handleUpdateDialogOpenClick = useCallback(()=>{
    if(selectedScoreName === ""){
      writeWarning('スコアを選択してください')
      return;
    }

    setUpdateDialogOpen(true);
  },[selectedScoreName]);


  const handleUpdated = useCallback( async ()=>{
    setUpdateDialogOpen(false);
    writeSuccess('スコアを更新しました');

    try{
      await updateTable();
    }
    catch(err){
      writeError('テーブルの更新に失敗しました');
    }
  },[]);
  const handleUpdateCancled = useCallback(()=>{
    setUpdateDialogOpen(false);
  },[]);

  const scoreTableElement = useMemo(()=>(
    <ScoreTable
      title={""}
      data={socreTableDatas}
      onSelectedChangeRow={handleTableSelectedChangeRow}
    />
  ),[socreTableDatas, handleTableSelectedChangeRow]);

  const [alertErrorOpen, setAlertErrorOpen] = React.useState(false);
  const [alertWarningOpen, setAlertWarningOpen] = React.useState(false);
  const [alertSuccessOpen, setAlertSuccessOpen] = React.useState(false);

  const [alertErrorText, setAlertErrorText] = React.useState("");
  const [alertWarningText, setAlertWarningText] = React.useState("");
  const [alertSuccessText, setAlertSuccessText] = React.useState("");

  const writeError = (text: string) => {
    setAlertErrorText(text);
    setAlertErrorOpen(true);
  };
  const writeWarning = (text: string) => {
    setAlertWarningText(text);
    setAlertWarningOpen(true);
  };
  const writeSuccess = (text: string) => {
    setAlertSuccessText(text);
    setAlertSuccessOpen(true);
  };

  useEffect(()=>{
    if(!alertSuccessOpen) return;

    const timerId = setTimeout(()=>{
      setAlertSuccessOpen(false);
    },5000);
    return ()=>{clearTimeout(timerId)};
  },[alertSuccessOpen, alertSuccessText]);

  useEffect(()=>{
    if(!alertWarningOpen) return;

    const timerId = setTimeout(()=>{
      setAlertWarningOpen(false);
    },5000);
    return ()=>{clearTimeout(timerId)};
  },[alertWarningOpen, alertWarningText]);

  return (
    <GenericTemplate title="スコアの一覧">
      <div style={{marginTop:'5px', marginBottom:'5px'}}>
        <Collapse in={alertErrorOpen}>
          <Alert
            severity="error"
            action={
              <IconButton
                aria-label="close"
                color="inherit"
                size="small"
                onClick={() => {
                  setAlertErrorOpen(false);
                }}
              >
                <CloseIcon fontSize="inherit" />
              </IconButton>
            }
          >
            {alertErrorText}
          </Alert>
        </Collapse>
        <Collapse in={alertWarningOpen}>
          <Alert
            severity="warning"
            action={
              <IconButton
                aria-label="close"
                color="inherit"
                size="small"
                onClick={() => {
                  setAlertWarningOpen(false);
                }}
              >
                <CloseIcon fontSize="inherit" />
              </IconButton>
            }
          >
            {alertWarningText}
          </Alert>
        </Collapse>
        <Collapse in={alertSuccessOpen}>
          <Alert
            severity="success"
            action={
              <IconButton
                aria-label="close"
                color="inherit"
                size="small"
                onClick={() => {
                  setAlertSuccessOpen(false);
                }}
              >
                <CloseIcon fontSize="inherit" />
              </IconButton>
            }
          >
            {alertSuccessText}
          </Alert>
        </Collapse>
      </div>
      <div>
        <Grid container spacing={3}>
          <Grid item xs={3}>
            <Button variant="outlined" color="primary" onClick={handleUpdateTable}>スコアの取得</Button>
          </Grid>
          <Grid item xs={3}>
            <Button variant="outlined" color="primary" onClick={handleUploadDialogOpenClick}>アップロードする</Button>
          </Grid>
          <Grid item xs={6} />
          {/*--------------------------------------------------*/}
          <Grid item xs={2}>
            <Button variant="outlined" color="primary" onClick={handleVersionDialogOpenClick}>表示</Button>
          </Grid>
          <Grid item xs={2}>
            <Button variant="outlined" color="primary" onClick={handleUpdateDialogOpenClick}>更新</Button>
          </Grid>
          <Grid item xs={8} />
          {/*--------------------------------------------------*/}
          <Grid item xs={12}>
            <div className={classes.table}>
              {scoreTableElement}
            </div>
          </Grid>
        </Grid>

        <UploadDialog
          open={uploadDialogOpen}
          onUploaded={handlerUploaded}
          onCanceled={handlerUploadCanceled}
        />

        <VersionDisplayDialog
          open={versionDialogOpen}
          scoreName={selectedScoreName}
          versions={displayScoreVersionList}
          onCloseClicked={handleVersionDialogCloseClicked}
        />

        <UpdateDialog
          open={updateDialogOpen}
          scoreName={selectedScoreName}
          onUploaded={handleUpdated}
          onCanceled={handleUpdateCancled}
        />

      </div>
    </GenericTemplate>
  )
}

export default UploadScorePage;
