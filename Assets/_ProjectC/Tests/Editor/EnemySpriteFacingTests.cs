using System.Collections; // 플레이 모드 전환 사용
using NUnit.Framework; // 몬스터 스프라이트 연결 검사 사용
using UnityEditor; // 실제 몬스터 에셋 조회 사용
using UnityEngine; // 몬스터 화면 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 몬스터 이미지 검사 사용
public sealed class EnemySpriteFacingTests // 몬스터 반전 스프라이트 연결 검사
{ // 몬스터 방향 검사 시작
    [UnityTest] // 실제 전투 몬스터 화면 검사 지정
    public IEnumerator RightFacingEnemiesUseSavedLeftFacingSprites() // 반전 스프라이트의 실제 전투 연결 확인
    { // 몬스터 방향 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 플레이 시작
        string[] names = { "AshSentinel", "AshBellkeeper", "BrokenScarecrow", "AshWanderer" }; // 반전이 필요한 몬스터 이름 목록
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 실제 전투 유닛 프리팹 조회
        Assert.IsNotNull(prefab); // 실제 전투 유닛 프리팹 존재 확인
        foreach (string name in names) // 반전이 필요한 몬스터 순회
        { // 몬스터 반전 화면 검사 시작
            string originalPath = "Assets/_ProjectC/Art/WinterfallEnemies/" + name + ".png"; // 보존할 원본 몬스터 이미지 경로
            string flippedPath = "Assets/_ProjectC/Art/WinterfallEnemies/" + name + "_Left.png"; // 새로 저장한 반전 이미지 경로
            string enemyPath = "Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_WF_" + name + ".asset"; // 실제 몬스터 데이터 경로
            Sprite original = AssetDatabase.LoadAssetAtPath<Sprite>(originalPath); // 기존 몬스터 이미지 조회
            Sprite flipped = AssetDatabase.LoadAssetAtPath<Sprite>(flippedPath); // 새 반전 스프라이트 조회
            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(enemyPath); // 실제 몬스터 데이터 조회
            Assert.IsNotNull(original, name + " 원본 이미지"); // 기존 원본 이미지 보존 확인
            Assert.IsNotNull(flipped, name + " 반전 이미지"); // 별도 반전 스프라이트 존재 확인
            Assert.IsNotNull(enemy, name + " 몬스터 데이터"); // 실제 몬스터 데이터 존재 확인
            Assert.AreNotSame(original, flipped); // 반전 이미지와 원본 분리 확인
            Assert.AreSame(flipped, enemy.Portrait); // 몬스터 데이터의 반전 스프라이트 연결 확인
            BattleUnitRuntime runtime = BattleUnitRuntime.CreateEnemy(enemy); // 실제 전투 몬스터 생성
            GameObject unitObject = Object.Instantiate(prefab); // 실제 전투 몬스터 화면 생성
            BattleUnitView view = unitObject.GetComponent<BattleUnitView>(); // 실제 전투 몬스터 화면 조회
            view.Bind(runtime); // 실제 몬스터 데이터를 전투 화면에 연결
            Image portrait = unitObject.transform.Find("Portrait").GetComponent<Image>(); // 실제 몬스터 그림 표시 조회
            Assert.AreSame(flipped, portrait.sprite); // 실제 전투 화면의 반전 스프라이트 표시 확인
            Object.DestroyImmediate(unitObject); // 임시 전투 몬스터 화면 정리
        } // 몬스터 반전 화면 검사 종료
        string[] unchanged = { "AshHunter", "AshTracker", "AshMaggot", "AshstormCore" }; // 기존 방향을 유지할 몬스터 이름 목록
        foreach (string name in unchanged) // 기존 방향 몬스터 순회
        { // 기존 몬스터 이미지 검사 시작
            Sprite original = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_ProjectC/Art/WinterfallEnemies/" + name + ".png"); // 기존 몬스터 이미지 조회
            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_WF_" + name + ".asset"); // 실제 몬스터 데이터 조회
            Assert.IsNotNull(original, name + " 기존 이미지"); // 기존 이미지 존재 확인
            Assert.IsNotNull(enemy, name + " 몬스터 데이터"); // 실제 몬스터 데이터 존재 확인
            Assert.AreSame(original, enemy.Portrait); // 기존 방향 몬스터의 원본 연결 유지 확인
        } // 기존 몬스터 이미지 검사 종료
        yield return new ExitPlayMode(); // 실제 전투 화면 플레이 종료
    } // 몬스터 방향 검사 종료
} // 몬스터 방향 검사 종료
