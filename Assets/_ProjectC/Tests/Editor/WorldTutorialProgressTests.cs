using System.Collections.Generic; // 독립 설정 저장소 사용
using NUnit.Framework; // 안내 진행 규칙 검사 사용
using UnityEngine; // 탐사 위치 검사 사용
public sealed class WorldTutorialProgressTests // 탐사와 거점 안내 규칙 검사
{ // 안내 규칙 검사 시작
    [Test] // 실제 탐사 이동 대기 검사 지정
    public void Exploration_RequiresMovementAndKeepsIndependentCompletion() // 실제 위치 변경과 안내별 완료 기록 확인
    { // 탐사 이동 검사 시작
        MemoryStore store = new MemoryStore(); // 독립 설정 저장소 생성
        WorldTutorialProgress tutorial = new WorldTutorialProgress(WorldTutorialKind.Exploration, store); // 탐사 안내 생성
        Assert.IsTrue(tutorial.StartIfNeeded(true, Vector2.zero)); // 첫 탐사 안내 시작 확인
        tutorial.RebasePosition(new Vector2(10f, 10f)); // 맵의 자동 시작 위치 배치 전달
        tutorial.ObservePosition(new Vector2(10f, 10f)); // 자동 배치 후 위치 확인
        Assert.IsFalse(tutorial.CanAdvance); // 맵 자동 배치를 플레이어 이동에서 제외 확인
        Assert.IsFalse(tutorial.Advance()); // 이동 전 다음 단계 차단 확인
        tutorial.ObservePosition(new Vector2(10.02f, 10f)); // 오차 범위 위치 변경 전달
        Assert.IsFalse(tutorial.CanAdvance); // 작은 위치 오차의 이동 미인정 확인
        tutorial.ObservePosition(new Vector2(10.2f, 10f)); // 실제 이동 거리 전달
        Assert.IsTrue(tutorial.CanAdvance); // 실제 이동 확인 후 다음 단계 허용 확인
        Assert.IsTrue(tutorial.Advance()); // 접촉 상호작용 안내 이동
        tutorial.Advance(); // 탐사 방 종류 안내 이동
        tutorial.Advance(); // 탐사 안내 완료
        Assert.IsFalse(tutorial.IsActive); // 탐사 안내 종료 확인
        Assert.AreEqual(1, store.Read("explorationTutorialCompleted", 0)); // 탐사 완료 기록 확인
        Assert.AreEqual(0, store.Read("lobbyTutorialCompleted", 0)); // 거점 안내 기록과 분리 확인
    } // 탐사 이동 검사 종료
    [Test] // 안내 설정과 다시 보기 검사 지정
    public void Lobby_SkipPersistsAndReplayWorksWhenDisabled() // 자동 안내와 건너뛰기 및 다시 보기 확인
    { // 거점 안내 검사 시작
        MemoryStore store = new MemoryStore(); // 독립 설정 저장소 생성
        WorldTutorialProgress tutorial = new WorldTutorialProgress(WorldTutorialKind.Lobby, store); // 거점 안내 생성
        Assert.IsFalse(tutorial.StartIfNeeded(false, Vector2.zero)); // 안내 설정이 꺼지면 자동 시작 차단 확인
        Assert.IsTrue(tutorial.StartIfNeeded(true, Vector2.zero)); // 안내 설정이 켜지면 첫 자동 시작 확인
        Assert.AreEqual(0, tutorial.StepIndex); // 첫 거점 안내 단계 확인
        Assert.IsTrue(tutorial.Advance()); // 다음 거점 안내 이동
        Assert.AreEqual(1, tutorial.StepIndex); // 거점 편성 설명 단계 확인
        tutorial.Skip(); // 거점 안내 건너뛰기
        Assert.AreEqual(1, store.Read("lobbyTutorialCompleted", 0)); // 건너뛰기 완료 기록 확인
        Assert.IsFalse(new WorldTutorialProgress(WorldTutorialKind.Lobby, store).StartIfNeeded(true, Vector2.zero)); // 완료한 안내 자동 재표시 차단 확인
        tutorial.Replay(Vector2.zero); // 설정과 완료 여부에 관계없이 다시 보기
        Assert.AreEqual(0, tutorial.StepIndex); // 거점 안내 처음부터 재시작 확인
    } // 거점 안내 검사 종료
    private sealed class MemoryStore : ISettingsStore // 안내 검사 전용 저장소
    { // 독립 저장소 시작
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(); // 임시 설정 값 목록
        public int Read(string key, int fallback) // 임시 설정 값 조회
        { // 설정 조회 시작
            return values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값 반환
        } // 설정 조회 종료
        public void Write(string key, int value) // 임시 설정 값 저장
        { // 설정 저장 시작
            values[key] = value; // 지정 설정 값 기록
        } // 설정 저장 종료
    } // 독립 저장소 종료
} // 안내 규칙 검사 종료
