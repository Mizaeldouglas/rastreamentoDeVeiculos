import { render, screen, fireEvent } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { VehicleList } from "./VehicleList";
import type { VehicleDto } from "../types/vehicle";

function buildVehicle(overrides: Partial<VehicleDto> = {}): VehicleDto {
  return {
    id: 1,
    plate: "ABC1234",
    model: "Fiat Strada",
    driver: "João",
    imei: null,
    ignitionOn: null,
    createdAt: "2026-01-01T00:00:00Z",
    lastPosition: null,
    ...overrides,
  };
}

describe("VehicleList", () => {
  it("shows the empty state when there are no vehicles", () => {
    render(<VehicleList vehicles={[]} onDelete={vi.fn()} />);

    expect(screen.getByText("Nenhum veículo cadastrado ainda.")).toBeInTheDocument();
  });

  it("renders vehicle data and calls onDelete when the remove button is clicked", () => {
    const onDelete = vi.fn();
    render(<VehicleList vehicles={[buildVehicle()]} onDelete={onDelete} />);

    expect(screen.getByText("ABC1234")).toBeInTheDocument();
    expect(screen.getByText("Simulado")).toBeInTheDocument();
    expect(screen.getByText("—")).toBeInTheDocument(); // ignitionOn null

    fireEvent.click(screen.getByText("✕ Remover"));
    expect(onDelete).toHaveBeenCalledWith(1);
  });

  it("shows the ignition badge state when known", () => {
    render(<VehicleList vehicles={[buildVehicle({ ignitionOn: true, imei: "123" })]} onDelete={vi.fn()} />);

    expect(screen.getByText("Ligado")).toBeInTheDocument();
    expect(screen.getByText("Dispositivo real (123)")).toBeInTheDocument();
  });
});
