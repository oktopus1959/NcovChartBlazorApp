#! /usr/bin/ruby

while line = gets
  items = line.strip.split('|');
  name = items[0].strip.split(/ +/)[0]
  tnum = items[2].strip.gsub(/,/, "").to_i
  if name == "東京"
    puts tnum
  end
end
