using System.Collections.Generic; // 선택 목록 사용
using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
using Object = UnityEngine.Object; // 객체 정리 이름 지정
public sealed class PartySelectionTests // 파티 선택 테스트
{ // 테스트 클래스 시작
    private sealed class StatusProvider : IPartyDeploymentStatusProvider // 테스트 출전 상태 제공자
    { // 상태 제공자 시작
        public CharacterData Blocked; // 출전 차단 캐릭터
        public bool IsDead(CharacterData character) => character == Blocked; // 사망 상태 조회
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 조회
        public bool CanDeploy(CharacterData character) => character != Blocked; // 출전 가능 상태 조회
    } // 상태 제공자 종료
    [Test] // 테스트 지정
    public void Selection_RejectsBlockedDuplicateAndFifthMember() // 출전 불가와 중복 인원 제한 확인
    { // 테스트 시작
        StatusProvider status = new StatusProvider(); // 출전 상태 제공자 생성
        PartySelection selection = new PartySelection(status); // 파티 선택 관리자 생성
        List<CharacterData> characters = new List<CharacterData>(); // 테스트 캐릭터 목록 생성
        for (int index = 0; index < 6; index++) // 캐릭터 여섯 명 생성
        { // 캐릭터 생성 시작
            characters.Add(ScriptableObject.CreateInstance<CharacterData>()); // 캐릭터 목록 추가
        } // 캐릭터 생성 종료
        status.Blocked = characters[0]; // 첫 캐릭터 출전 차단
        Assert.IsFalse(selection.TryAdd(characters[0])); // 출전 불가 선택 차단
        for (int index = 1; index <= 4; index++) // 출전 가능 네 명 선택
        { // 파티 선택 시작
            Assert.IsTrue(selection.TryAdd(characters[index])); // 정상 선택 확인
        } // 파티 선택 종료
        Assert.IsFalse(selection.TryAdd(characters[1])); // 중복 선택 차단
        Assert.IsFalse(selection.TryAdd(characters[5])); // 다섯 번째 선택 차단
        IReadOnlyList<CharacterData> members = selection.Members; // 현재 선택 목록 조회
        Assert.AreEqual(4, members.Count); // 네 명 유지 확인
        Assert.AreSame(characters[1], members[0]); // 선택 순서 유지 확인
        Assert.IsTrue(selection.TryRemove(characters[1])); // 선택 해제 확인
        Assert.IsTrue(selection.TryAdd(characters[5])); // 빈 자리 재편성 확인
        foreach (CharacterData character in characters) // 생성 캐릭터 순회
        { // 테스트 객체 정리 시작
            Object.DestroyImmediate(character); // 캐릭터 객체 정리
        } // 테스트 객체 정리 종료
    } // 테스트 종료
    [Test] // 테스트 지정
    public void Validation_DetectsStateChangesAfterSelection() // 선택 후 출전 상태 변화 확인
    { // 테스트 시작
        StatusProvider status = new StatusProvider(); // 출전 상태 제공자 생성
        PartySelection selection = new PartySelection(status); // 파티 선택 관리자 생성
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 테스트 캐릭터 생성
        Assert.IsTrue(selection.TryAdd(character)); // 생존 캐릭터 선택 확인
        status.Blocked = character; // 선택 후 사망 상태 변경
        PartyDeploymentValidationResult result = selection.Validate(); // 출발 직전 상태 검증
        Assert.IsFalse(result.CanDeploy); // 사망 캐릭터 출전 차단 확인
        Assert.AreEqual(PartyDeploymentBlockReason.DeadMember, result.BlockReason); // 사망 차단 사유 확인
        Object.DestroyImmediate(character); // 캐릭터 객체 정리
    } // 테스트 종료
} // 테스트 클래스 종료
