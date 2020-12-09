import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Grid, MenuItem, Select} from "@material-ui/core";
import React, { useCallback, useEffect } from "react";
import PracticeManagerApiClient from "../../PracticeManagerApiClient";
import ScoreViewr from "../atoms/ScoreViewer";

const client = new PracticeManagerApiClient("http://localhost:5000/");

const downloadImageUrls = async (name: string, version: number): Promise<string[]> => {
  console.log(`changeImageUrls ${name}:${version}`);
  if(name === ''){
    return [];
  }
  if(version < 0){
    return [];
  }
  console.log(`${name}:${version}`);
  const scoreVersion = await client.getScoreVersion(name, version);
  return scoreVersion.pages.map(x=>x.url);
};

export interface VersionDisplayDialogProps{
  open: boolean;
  scoreName: string;
  versions: number[];
  onCloseClicked?: ((event: {}) => void) | undefined;

}

const VersionDisplayDialog = (props: VersionDisplayDialogProps) => {

  const open = props.open;
  const scoreName = props.scoreName;
  const versions = props.versions;
  const iniVersion = 0 < versions.length ? versions.slice(-1)[0] : 0;
  const onCloseClicked = props.onCloseClicked;


  const [imageUrls, setImageUrls] = React.useState([] as string[]);
  const [displayScoreVersion, setDisplayScoreVersion] = React.useState(0);


  useEffect(()=>{
    const init = async () =>{
      const urls = await downloadImageUrls(scoreName, displayScoreVersion);
      setImageUrls(urls);
    };

    init();
  },[displayScoreVersion, scoreName]);

  useEffect(()=>{
    setDisplayScoreVersion(iniVersion);
  },[iniVersion]);


  const changeImageUrls = async (name: string, version: number) => {
    if(name === ''){
      return;
    }
    if(version < 0){
      return;
    }
    const urls = await downloadImageUrls(name, version);
    setImageUrls(urls);
  };



  const handleVersionDialogClose = useCallback(async ()=>{
    if(onCloseClicked){
      onCloseClicked({});
    }
  },[onCloseClicked]);

  useEffect(()=>{
    const f = async ()=>{
      await changeImageUrls(scoreName, displayScoreVersion);
    };

    f();

  },[scoreName, displayScoreVersion]);

  const handleDisplayChange = useCallback(async (event)=>{
    const version = event.target.value;;
    setDisplayScoreVersion(version);

  }, []);

  const versionDialog = (
    <Dialog
      fullScreen
      open={open}
      onClose={handleVersionDialogClose}
      scroll={'paper'}
    >
      <DialogTitle>バージョン表示</DialogTitle>
      <DialogContent dividers={true}>
        <Grid container spacing={3} style={{height:"100%"}}>
          <Grid item xs={12} style={{textAlign: "center", height:"auto"}}>
            <Select
              onChange={handleDisplayChange}
              value={displayScoreVersion}
            >
              {
                versions.map((x,i)=>(
                <MenuItem value={x} key={i.toString()}>{x.toString()}</MenuItem>
                ))
              }
            </Select>
          </Grid>
          <Grid item xs={12} style={{textAlign: "center", height:"90%"}}>
            <div id="score-viewer-container" style={{width: "90vw", height: "65vh", display: "inline-block"}} >
              <ScoreViewr imageUrls={imageUrls}></ScoreViewr>
            </div>
          </Grid>
        </Grid>

      </DialogContent>
      <DialogActions>
        <Button variant="outlined" color="primary" onClick={handleVersionDialogClose}>閉じる</Button>
      </DialogActions>
    </Dialog>
  );

  return versionDialog;

}

export default VersionDisplayDialog;
