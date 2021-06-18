import { useCallback, useEffect, useState } from "react";
import { scoreClientV2 } from "../../global";
import { ScoreSnapshotDetail } from "../../ScoreClientV2";

const getDetail = async (
  scoreId?: string,
  snapshotId?: string
): Promise<ScoreSnapshotDetail | undefined> => {
  if (scoreId === undefined) {
    return;
  }
  if (snapshotId === undefined) {
    return;
  }
  try {
    const response = await scoreClientV2.getSnapshotDetail(scoreId, snapshotId);
    return response;
  } catch (err) {
    console.log(err);
    throw err;
  }
};

export interface useMeyScoreSnapshotDetailProps {
  scoreId?: string;
  snapshotId?: string;
  retryCount?: number;
  retryIntarval?: number;
}

export default function useMeyScoreSnapshotDetail(
  props: useMeyScoreSnapshotDetailProps
): [ScoreSnapshotDetail | undefined, () => void] {
  const _scoreId = props.scoreId;
  const _snapshotId = props.snapshotId;
  const _retryCount = props.retryCount;
  const _retryIntarval = Math.max(props.retryIntarval ?? 0, 1000);
  const [detail, setDetail] = useState<ScoreSnapshotDetail | undefined>();
  const [count, setCount] = useState<number | undefined>();
  const [end, setEnd] = useState<boolean>(false);
  const [reloadCount, setReloadCount] = useState<number>(0);

  const update = useCallback(() => {
    setReloadCount(reloadCount + 1);
  }, [reloadCount]);

  useEffect(() => {
    const f = async () => {
      try {
        const response = await getDetail(_scoreId, _snapshotId);
        setDetail(response);
        setEnd(true);
      } catch (err) {}
    };

    setCount(_retryCount);
    setEnd(false);

    f();
  }, [_scoreId, reloadCount]);

  useEffect(() => {
    if (end) {
      return;
    } else if (count === undefined) {
      return;
    } else if (count === 0) {
      return;
    }

    const timerId = setTimeout(async () => {
      try {
        console.log(`retry: ${count}`);
        const response = await getDetail(_scoreId);
        setDetail(response);
        setEnd(true);
      } catch (err) {
        setCount(count - 1);
      }
    }, _retryIntarval);

    return () => {
      clearTimeout(timerId);
    };
  }, [count, end]);

  return [detail, update];
}
