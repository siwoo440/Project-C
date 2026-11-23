using NUnit.Framework; // 테스트 기능 사용
using UnityEditor; // 테스트 덱 에셋 조회
using UnityEngine; // 유니티 객체 사용
public sealed class RunDeckRemovalRulesTests // 회차 덱 카드 제거 규칙 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void RemoveCard_CannotReduceDeckBelowTen() // 최소 덱 열 장 유지 확인
    { // 테스트 시작
        DeckData source = AssetDatabase.LoadAssetAtPath<DeckData>("Assets/_ProjectC/ScriptableObjects/Decks/Deck_Test.asset"); // 기존 테스트 덱 조회
        Assert.IsNotNull(source); // 테스트 덱 존재 확인
        GameObject managerObject = new GameObject("RunDeckRemovalTest"); // 테스트 관리자 생성
        RunDeckManager manager = managerObject.AddComponent<RunDeckManager>(); // 회차 덱 관리자 연결
        manager.ResetToSource(source); // 테스트 덱 복사
        Assert.Greater(manager.CardCount, 10); // 충분한 테스트 카드 확인
        while (manager.CardCount > 11) // 열한 장까지 카드 제거
        { // 카드 제거 반복 시작
            Assert.IsTrue(manager.TryRemoveAt(0)); // 제거 성공 확인
        } // 카드 제거 반복 종료
        Assert.IsTrue(manager.TryRemoveAt(0)); // 열한 장에서 한 장 제거 확인
        Assert.AreEqual(10, manager.CardCount); // 열 장 잔여 확인
        Assert.IsFalse(manager.CanRemoveAt(0)); // 추가 제거 차단 확인
        Assert.IsFalse(manager.TryRemoveAt(0)); // 실제 제거 차단 확인
        Assert.AreEqual(10, manager.CardCount); // 최소 카드 수 유지 확인
        Object.DestroyImmediate(managerObject); // 테스트 관리자 정리
    } // 테스트 종료
} // 테스트 클래스 종료
