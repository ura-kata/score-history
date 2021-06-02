import React from "react";
import clsx from "clsx";
import {
  colors,
  createMuiTheme,
  createStyles,
  CssBaseline,
  makeStyles,
  Theme,
  ThemeProvider,
  Typography,
  AppBar,
  Toolbar,
  IconButton,
  Drawer,
  Divider,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Container,
  Box,
  Badge,
} from "@material-ui/core";
import MenuIcon from "@material-ui/icons/Menu";
import ChevronLeftIcon from "@material-ui/icons/ChevronLeft";
import CloudUploadIcon from "@material-ui/icons/CloudUpload";
import ViewCarouselIcon from "@material-ui/icons/ViewCarousel";
import ExtensionIcon from "@material-ui/icons/Extension";
import HomeIcon from "@material-ui/icons/Home";
import { Link } from "react-router-dom";
import Copyright from "../atoms/Copyright";
import AppBarIcons from "../molecules/AppBarIcons";
import { AppContext, AppContextDispatch } from "../../AppContext";

const drawerWidth = 240;

const theme = createMuiTheme({
  typography: {
    fontFamily: ["Noto Sans JP", "游ゴシック体"].join(","),
  },
  palette: {
    primary: { main: colors.blueGrey[800] },
  },
});

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      display: "flex",
    },
    appBar: {
      zIndex: theme.zIndex.drawer + 1,
      transition: theme.transitions.create(["width", "margin"], {
        easing: theme.transitions.easing.sharp,
        duration: theme.transitions.duration.leavingScreen,
      }),
    },
    appBarShift: {
      marginLeft: drawerWidth,
      width: `calc(100% - ${drawerWidth}px)`,
      transition: theme.transitions.create(["width", "margin"], {
        easing: theme.transitions.easing.sharp,
        duration: theme.transitions.duration.enteringScreen,
      }),
    },
    toolbar: {
      paddingRight: 24,
    },
    menuButton: {
      marginRight: 36,
    },
    menuButtonHidden: {
      display: "none",
    },
    title: {
      flexGrow: 1,
    },
    drawerPaper: {
      position: "relative",
      whiteSpace: "nowrap",
      width: drawerWidth,
      transition: theme.transitions.create("width", {
        easing: theme.transitions.easing.sharp,
        duration: theme.transitions.duration.enteringScreen,
      }),
    },
    drawerPaperClose: {
      overflowX: "hidden",
      transition: theme.transitions.create("width", {
        easing: theme.transitions.easing.sharp,
        duration: theme.transitions.duration.leavingScreen,
      }),
      width: theme.spacing(7),
      [theme.breakpoints.up("sm")]: {
        width: theme.spacing(9),
      },
    },
    toolbarIcon: {
      display: "flex",
      alignItems: "center",
      justifyContent: "flex-end",
      padding: "0 8px",
      ...theme.mixins.toolbar,
    },
    link: {
      textDecoration: "none",
      color: theme.palette.text.secondary,
    },
    content: {
      flexGrow: 1,
      height: "100vh",
      overflow: "auto",
    },
    appBarSpacer: theme.mixins.toolbar,
    container: {
      paddingTop: theme.spacing(4),
      paddingBottom: theme.spacing(4),
    },
    pageTitle: {
      marginBottom: theme.spacing(1),
    },
    grow: {
      flexGrow: 1,
    },
  })
);

export interface GenericTemplateProps {
  children: React.ReactNode;
  title?: string;
}

const GenericTemplate = (props: GenericTemplateProps) => {
  const _children = props.children;
  const _title = props.title;

  const appContext = React.useContext(AppContext);
  const appContextDispatch = React.useContext(AppContextDispatch);

  const _navigationOpen = appContext.navigationOpen;
  const _userMe = appContext.userMe;

  const classes = useStyles();

  const handleDrawerOpen = () => {
    appContextDispatch({ type: "openNavi" });
  };
  const handleDrawerClose = () => {
    appContextDispatch({ type: "closeNavi" });
  };

  const selectedPage = (window.location.pathname + "/")
    .split("/")
    .slice(1)[0]
    .toLowerCase();

  return (
    <ThemeProvider theme={theme}>
      <div className={classes.root}>
        <CssBaseline />
        <AppBar
          position="absolute"
          className={clsx(
            classes.appBar,
            _navigationOpen && classes.appBarShift
          )}
        >
          <Toolbar className={classes.toolbar}>
            <IconButton
              edge="start"
              color="inherit"
              aria-label="open drawer"
              onClick={handleDrawerOpen}
              className={clsx(
                classes.menuButton,
                _navigationOpen && classes.menuButtonHidden
              )}
            >
              <MenuIcon />
            </IconButton>
            <Typography
              component="h1"
              variant="h6"
              color="inherit"
              noWrap
              className={classes.title}
            >
              Practice Manager
            </Typography>

            <div className={classes.grow} />

            <AppBarIcons userMe={_userMe} />
          </Toolbar>
        </AppBar>

        <Drawer
          variant="permanent"
          classes={{
            paper: clsx(
              classes.drawerPaper,
              !_navigationOpen && classes.drawerPaperClose
            ),
          }}
          open={_navigationOpen}
        >
          <div className={classes.toolbarIcon}>
            <IconButton onClick={handleDrawerClose}>
              <ChevronLeftIcon />
            </IconButton>
          </div>
          <Divider />
          <List>
            <Link to="/" className={classes.link}>
              <ListItem
                button
                selected={selectedPage === "" || selectedPage === "home"}
              >
                <ListItemIcon>
                  <HomeIcon />
                </ListItemIcon>
                <ListItemText primary="ホーム" />
              </ListItem>
            </Link>
            <Link to="/new" className={classes.link}>
              <ListItem button selected={selectedPage === "new"}>
                <ListItemIcon>
                  <CloudUploadIcon />
                </ListItemIcon>
                <ListItemText primary="スコアの作成" />
              </ListItem>
            </Link>
            <Link to="/upload" className={classes.link}>
              <ListItem button selected={selectedPage === "upload"}>
                <ListItemIcon>
                  <CloudUploadIcon />
                </ListItemIcon>
                <ListItemText primary="スコアのアップロード" />
              </ListItem>
            </Link>
            <Link to="/display" className={classes.link}>
              <ListItem button selected={selectedPage === "display"}>
                <ListItemIcon>
                  <ViewCarouselIcon />
                </ListItemIcon>
                <ListItemText primary="スコアの表示" />
              </ListItem>
            </Link>
            <Link to="/api-test" className={classes.link}>
              <ListItem button selected={selectedPage === "api-test"}>
                <ListItemIcon>
                  <ExtensionIcon />
                </ListItemIcon>
                <ListItemText primary="API Test" />
              </ListItem>
            </Link>
          </List>
        </Drawer>
        <main className={classes.content}>
          <div className={classes.appBarSpacer} />
          <Container maxWidth="lg" className={classes.container}>
            {_title ? (
              <Typography
                variant="h5"
                color="inherit"
                noWrap
                className={classes.pageTitle}
              >
                {_title}
              </Typography>
            ) : (
              <></>
            )}
            {_children}
            <Box pt={4}>
              <Copyright />
            </Box>
          </Container>
        </main>
      </div>
    </ThemeProvider>
  );
};

export default GenericTemplate;
