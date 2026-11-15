using NUnit.Framework; // 캐릭터 해금 검증 사용
using UnityEngine; // 캐릭터 리소스 사용

public sealed class PartyUnlockTests // 파티 출전과 세계 해금 검증
{ // 해금 검증 시작
    private sealed class AvailableStatus : IPartyDeploymentStatusProvider // 테스트 출전 상태 제공자
    { // 출전 상태 제공자 시작
        public bool IsDead(CharacterData character) => false; // 사망 상태 없음
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음
        public bool CanDeploy(CharacterData character) => true; // 출전 가능 상태
    } // 출전 상태 제공자 종료
    [Test] // 편집기 테스트 지정
    public void WorldUnlock_PersistsAndAllowsPartySelection() // 세계 해금 저장과 편성 허용 확인
    { // 세계 해금 검증 시작
        string key = "ProjectC.Test.Unlock." + System.Guid.NewGuid().ToString("N"); // 독립 진행 상태 키 생성
        try // 테스트 진행 상태 정리 보장
        { // 세계 해금 확인 시작
            CharacterUnlockProgress progress = new CharacterUnlockProgress(key); // 독립 해금 상태 생성
            CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 전체 캐릭터 목록 조회
            CharacterData world = roster.Characters[21]; // 세계 캐릭터 조회
            PartySelection selection = new PartySelection(new AvailableStatus(), progress); // 독립 해금 상태의 편성 생성
            Assert.IsFalse(selection.TryAdd(world)); // 해금 전 세계 편성 차단 확인
            progress.RecordFinalClear(); // 최종 탐사 성공 기록
            CharacterUnlockProgress reloaded = new CharacterUnlockProgress(key); // 저장된 진행 상태 재조회
            Assert.IsTrue(reloaded.IsUnlocked(world)); // 재생성 후 세계 해금 유지 확인
            Assert.IsTrue(selection.TryAdd(world)); // 해금 후 세계 편성 허용 확인
            Assert.AreSame(world, selection.Members[0]); // 선택된 세계 캐릭터 확인
            Assert.IsTrue(selection.Validate().CanDeploy); // 세계 출전 허용 확인
        } // 세계 해금 확인 종료
        finally // 테스트 상태 정리 시작
        { // 독립 저장 상태 제거 시작
            PlayerPrefs.DeleteKey(key); // 테스트 전용 진행 키 제거
            PlayerPrefs.Save(); // 테스트 키 제거 저장
        } // 독립 저장 상태 제거 종료
    } // 세계 해금 검증 종료
} // 해금 검증 종료
