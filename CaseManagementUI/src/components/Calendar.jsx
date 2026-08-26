import { useCallback, useEffect, useState } from "react";

import api from "../services/http";

const toDateOnly = (d) => d.toISOString().slice(0, 10);

export default function Calendar() {
  const [currentDate, setCurrentDate] = useState(new Date());
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const year = currentDate.getFullYear();
  const month = currentDate.getMonth() + 1;

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      setError(null);

      const startDate = toDateOnly(new Date(year, month - 1, 1));
      const endDate = toDateOnly(new Date(year, month, 0));

      const res = await api.post("/api/corqs", {
        action: "getCalendar",
        params: { startDate, endDate },
      });

      setEvents(res.data?.data ?? []);
    } catch (err) {
      console.error("Error fetching calendar:", err);
      setEvents([]);
      setError("Failed to load calendar.");
    } finally {
      setLoading(false);
    }
  }, [year, month]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const prevMonth = () =>
    setCurrentDate(new Date(year, month - 2, 1));

  const nextMonth = () =>
    setCurrentDate(new Date(year, month, 1));

  const firstDay = new Date(year, month - 1, 1).getDay();
  const daysInMonth = new Date(year, month, 0).getDate();

  const eventsForDay = (day) =>
    events.filter(e =>
      new Date(e.start).getDate() === day
    );

  return (
    <div className="p-6 bg-gray-100 min-h-screen flex justify-center">
      <div className="w-full max-w-6xl bg-white p-6 rounded shadow">
        <div className="flex justify-between items-center mb-4">
          <button onClick={prevMonth}>‹</button>
          <h2 className="text-xl font-semibold">
            {currentDate.toLocaleString("default", {
              month: "long",
              year: "numeric"
            })}
          </h2>
          <button onClick={nextMonth}>›</button>
        </div>

        {error && <p className="text-red-500">{error}</p>}
        {loading ? (
          <p>Loading calendar…</p>
        ) : (
          <div className="grid grid-cols-7 gap-2">
            {["Sun","Mon","Tue","Wed","Thu","Fri","Sat"].map(d => (
              <div key={d} className="text-center font-semibold">
                {d}
              </div>
            ))}

            {Array.from({ length: firstDay }).map((_, i) => (
              <div key={`empty-${i}`} />
            ))}

            {Array.from({ length: daysInMonth }).map((_, i) => {
              const day = i + 1;
              const dayEvents = eventsForDay(day);

              return (
                <div
                  key={day}
                  className="border rounded min-h-[110px] p-2 bg-gray-50"
                >
                  <div className="font-semibold text-sm mb-1">{day}</div>

                  {dayEvents.map(e => (
                    <div
                      key={e.eventId}
                      title={`${e.provider ?? ""} — ${e.status ?? ""}`}
                      className="text-xs bg-blue-100 rounded px-1 py-0.5 mb-1"
                    >
                      {new Date(e.start).toLocaleTimeString([], { hour: "numeric", minute: "2-digit" })}{" "}
                      {e.title}
                    </div>
                  ))}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
