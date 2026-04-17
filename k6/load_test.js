/**
 * Load тест — перевірка поведінки API студентів під стандартним навантаженням.
 *
 * Мета: оцінити продуктивність при очікуваній кількості користувачів.
 * Профіль навантаження:
 *   1 хв  — розгін до 10 VUs
 *   3 хв  — стабільне навантаження 10 VUs
 *   1 хв  — плавне зниження до 0
 *
 * Запуск: k6 run k6/load_test_ramp.js
 */

import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    get_students_10000: {
      executor: "shared-iterations",
      vus: 50,
      iterations: 10000,
      maxDuration: "4m",
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<500"], // 95% запитів мають відповідати менш ніж за 500мс
    http_req_failed: ["rate<0.01"], // помилок повинно бути менше 1%
  },
};

const API_URL = __ENV.API_URL || "http://localhost:5111/api/student";

export default function () {
  // Отримати всіх студентів
  const getRes = http.get(API_URL);
  check(getRes, {
    "GET /api/student returns 200": (r) => r.status === 200,
  });

  // Створити нового студента
  const studentData = JSON.stringify({
    fullName: `Load Test Student ${__VU}-${__ITER}`,
    email: `student${__VU}_${__ITER}@loadtest.com`,
    enrollmentDate: new Date().toISOString(),
  });
  const createRes = http.post(API_URL, studentData, {
    headers: { "Content-Type": "application/json" },
  });
  check(createRes, {
    "POST /api/student returns 201": (r) => r.status === 201,
  });

  // Якщо створено, отримати та видалити
  if (createRes.status === 201) {
    const student = createRes.json();
    const id = student.id || student.ID || student.Id;

    // Отримати студента за ID
    const getByIdRes = http.get(`${API_URL}/${id}`, {
      tags: { name: "GET /api/student/{id}" },
    });
    check(getByIdRes, {
      "GET /api/student/{id} returns 200": (r) => r.status === 200,
    });

    // Видалити студента
    const delRes = http.del(`${API_URL}/${id}`, null, {
      tags: { name: "DELETE /api/student/{id}" },
    });
    check(delRes, {
      "DELETE /api/student/{id} returns 200/204": (r) =>
        r.status === 200 || r.status === 204,
    });
  }

  sleep(1);
}
